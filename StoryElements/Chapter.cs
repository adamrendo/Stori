using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace StoryElements {

    // A chapter is defined as a collection of scenes.
    public class Chapter : StoryElement {
    
        private List<Scene> scenes;
        public Scene scene;

        public Chapter(Story story, List<string> rawLines) : base(story, rawLines) {

            Dictionary<string,string> attributes = ProcessAttributes(rawLines, 1);
            foreach(KeyValuePair<string, string> attribute in attributes) {
                switch(attribute.Key) {
                    case "prereq":
                    case "postreq":
                    break;
                    default:
                    Debug.LogWarning("Unknown chapter attribute "+attribute.Key+" spotted.");
                    break;
                }
            }

            this.scenes = new List<Scene>();
            List<string> sceneRawLines = new List<string>(); 
            foreach(string line in rawLines.Skip(1+attributes.Count)) {
                int tabAmount = line.Count(f => f== '\t');
                if(tabAmount == 1) { // line is beginning of a scene
                    if(sceneRawLines.Count > 0)
                        scenes.Add(new Scene(story, sceneRawLines));
                    sceneRawLines = new List<string>();
                }
                sceneRawLines.Add(line); // line is part of a scene
            }
            scenes.Add(new Scene(story, sceneRawLines));

            scene = scenes[0]; //sets current scene to first scene.

        }

        public void NextSceneByOption(ushort optionId) {
            //Find and go to next scene based on current scene and picked option targetID
            if(OnLastScene())
                Debug.LogError("You cannot end a chapter with a scene that has options.");
            ushort target = scene.GetOption(optionId).GetTargetId();
            if(target==0) {
                Debug.Log("This option has no target, which means I'll send you to the next scene determined by the scene not the option.");
                NextSceneByScene();
                return;
            }
            GotoSceneById(target);
            
        }

        public void NextSceneByScene() {
            //Find and go to next scene based on current scene and scene targetID
            if(!scene.HasTarget())
                GotoSceneByIndex(scenes.IndexOf(scene)+1);
            else
                GotoSceneById(scene.GetTargetId());
        }

        public void GotoSceneById(ushort index) {
            //Find and go to scene solely based on scene list index (not defined ID)
            foreach(Scene s in scenes) {
                if(s.GetId()==index)
                    GotoSceneByIndex(scenes.IndexOf(s));
            }   
        }

        private void GotoSceneByIndex(int index) {
            //Find and actually go to scene based on list index. This is the final step in the 'next scene' funnel.
            Scene desiredScene = scenes[index];
            if(desiredScene.MatchesPrereqs(story.GetChoices())) {
                scene = desiredScene;
                scene.OnLoad();
            } else {
                Debug.Log("You do not have the prerequisites necessary for this scene. Sending you one over. Hopefully you end up where you need to.");
                Debug.Log(string.Join(", ", story.GetChoices()));
                Debug.Log(string.Join(", ", desiredScene.GetPrereqs()));
                Debug.Log(String.Format("You have: {0}, but you need {1}",string.Join(", ", story.GetChoices()),string.Join(", ", desiredScene.GetPrereqs())));
                GotoSceneByIndex(index+1);
            }

        }

        public bool OnLastScene() {
            //Is this the last scene?
            return scenes.IndexOf(scene) == scenes.Count-1;
        }

    }
}