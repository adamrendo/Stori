using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace StoryElements {

    //The Story class holds all chapters and serves as a head compiler for the stori code.
    public class Story {

        private List<Chapter> chapters;
        private List<string> choices;
        private Dictionary<string,string> variables;
        private Chapter chapter;
        private int score;

        public Story(string filePath) {

            score = 0;
            variables = InitVariables();
            chapters = new List<Chapter>();
            choices = new List<string>();

            // Read file
            Debug.Log("Reading story file");
            string[] rawLines = File.ReadAllLines(filePath);

            // Pre-process raw stori file
            Debug.Log("Preprocessing lines");
            List<string> lines = Preprocess(rawLines);

            // Parse stori file into scene objects
            Debug.Log("Parsing story file into objects");
            List<string> elementLines = new List<string>();
            foreach(string line in lines) {
                int tabAmount = line.Count(f => f== '\t');
                if(tabAmount == 0) { // line is beginning of a chapter
                    if(elementLines.Count > 0)
                        chapters.Add(new Chapter(this, elementLines));
                    elementLines = new List<string>();
                }
                elementLines.Add(line); // line is part of a chapter
            }
            if(elementLines.Count > 0)
                chapters.Add(new Chapter(this, elementLines));

            chapter = chapters[0]; // set current chapter to first chapter.

        }

        private Dictionary<string,string> InitVariables() {
            // define your initial story variables (which can be overwritten in-stori later) here
            return new Dictionary<string, string>() {
                {"alvaRuler", "Queen Braelyn"}
            };

        }

        private List<string> Preprocess(string[] rawLines) {
            // compile most of stori file
            List<string> newLines = new List<string>();
            for(int i = 0; i < rawLines.Length; i++) {
                string line = rawLines[i];

                // Remove comments
                if(line.Contains("#")) {
                    int index = line.IndexOf('#');
                    if(index==0)
                        continue;
                    line = line.Substring(0,index);
                }

                // Remove empty lines (this goes after removing comments so comment-only lines are also removed)
                int tabAmount = line.Count(f => f== '\t');
                int spaceAmount = line.Count(f => f== ' ');
                if((line.Length-tabAmount-spaceAmount)<=0)
                    continue;

                // Remove chapter function words
                if(tabAmount==0) {
                    if(line.Contains("End"))
                        continue;
                    if(line.Contains("Chapter")) {
                        line = line.Substring(line.IndexOf(' ')+1);
                    }
                }
            
                string ReplaceTag(string line, string tagName, string colorReference, string fontReference) {

                    string tagOpen = "", tagClose = "";
                    if(colorReference!=null) tagOpen += "<color="+colorReference+">";
                    if(fontReference!=null) {
                        tagOpen += "<font="+fontReference+">";
                        tagClose += "</font>";
                    }
                    if(colorReference!=null) tagClose += "</color>";

                    line = line.Replace("<"+tagName+">",tagOpen);
                    line = line.Replace("</"+tagName+">",tagOpen);
                    return line;

                }

                string ReplaceName(string line, string[] names, string colorReference) {

                    foreach(string name in names) 
                        line = line.Replace(name,"<color="+colorReference+">"+name+"</color>");
                    return line;

                }

                //Color and font code tags
                line = ReplaceTag(line, "tut", TextReference.COLOR_TUTORIAL, TextReference.FONT_TUTORIAL); //example custom tag to replace with color and font rich text tags
                line = ReplaceTag(line, "Ed", TextReference.COLOR_EVERETTDIA, null);
                line = ReplaceTag(line, "Od", TextReference.COLOR_OMARDIA, null);
                line = ReplaceTag(line, "Pd", TextReference.COLOR_PRINCESSDIA, null);
                line = ReplaceTag(line, "Qd", TextReference.COLOR_QUEENDIA, null);

                //Colored names
                line = ReplaceName(line, new string[] {"General Everett","Everett","General Horrison"}, TextReference.COLOR_EVERETT); //used to make names colorful. Could also use this to make concepts highlighted.
                line = ReplaceName(line, new string[] {"Omar","Omar Vellas"}, TextReference.COLOR_OMAR);
                line = ReplaceName(line, new string[] {"Amelise","Princess Amelise", "the Princess"}, TextReference.COLOR_PRINCESS);
                line = ReplaceName(line, new string[] {"the Queen","Queen Braelyn", "Braelyn"}, TextReference.COLOR_QUEEN);

                newLines.Add(line);

            }
            return newLines;

        }

        public void SelectOption(ushort optionId) {

            Option option = chapter.scene.GetOption(optionId);
            OnCloseScene(option);
            chapter.NextSceneByOption(optionId);

        }

        public void OnCloseScene(Option option) {
            // Run whatever you must to handle the end of a scene
            if(option!=null) {
                if(option.GetConseqs() != null) { // add option consequences
                    choices.AddRange(option.GetConseqs());
                    Debug.Log(String.Format("Added option [{0}] to choices made.", string.Join(", ", option.GetConseqs())));
                    Debug.Log(String.Format("Choices now contains: [{0}].", string.Join(", ", choices)));
                }
                if(option.GetOverrides() != null) { // apply option override variables
                    foreach(KeyValuePair<string,string> variable in option.GetOverrides()) {
                        if(variables.ContainsKey(variable.Key)) // override
                            variables[variable.Key] = variable.Value;
                        else // add new
                            variables.Add(variable.Key, variable.Value);
                    }
                }
            }

            if(chapter.scene.GetConseqs() != null) { // add scene consequences
                choices.AddRange(chapter.scene.GetConseqs());
                Debug.Log(String.Format("Added scene [{0}] to choices made.", string.Join(", ", chapter.scene.GetConseqs())));
                Debug.Log(String.Format("Choices now contains: [{0}].", string.Join(", ", choices)));
            }
            if(chapter.scene.GetOverrides() != null) { // apply scene override variables
                foreach(KeyValuePair<string,string> variable in chapter.scene.GetOverrides()) {
                    if(variables.ContainsKey(variable.Key)) // override
                        variables[variable.Key] = variable.Value;
                    else // add new
                        variables.Add(variable.Key, variable.Value);
                }
            }

            //score
            score += chapter.scene.GetScore() + ((option!=null)?option.GetScore():0);

        }

        public bool TryContinue() {
            // Tries to just continue to the next scene (could use this to press space or any key to progress through scenes without any required input (options))
            if(!chapter.scene.HasOptions()) {
                OnCloseScene(null);
                if(chapter.OnLastScene()) {
                    NextChapter();
                    Debug.Log("Fear not, this is just a new chapter.");
                }
                else
                    chapter.NextSceneByScene();
                return true;
            }
            return false;
        }

        public void HesitatedContinue() {
            // When the timer runs out
            OnCloseScene(null);
            if(chapter.scene.GetTimerOption() == 0)
                chapter.NextSceneByScene();
            else
                chapter.GotoSceneById(chapter.scene.GetTimerOption());
        }

        private void NextChapter() {
            // Currently chapters don't have id's like scenes do. Could change this.
            chapter = chapters[chapters.IndexOf(chapter)+1];
        }

        public List<string> GetChoices() {
            return choices;
        }

        public Scene GetCurrentScene() {
            return chapter.scene;
        }

        public Dictionary<string,string> GetVariables() {
            return variables;
        }

    }
}