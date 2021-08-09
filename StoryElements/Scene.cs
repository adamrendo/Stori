using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace StoryElements {

    // A Scene is the core story element. It is part of a chapter and can have options to define the choice to make.
    public class Scene : StoryElement {

        private List<Option> options;
        private float timerMax, shakeAmount;
        private ushort timerOption;
        private string location, sound;

        public Scene(Story story, List<string> rawLines) : base(story, rawLines) {

            Dictionary<string,string> attributes = ProcessAttributes(rawLines, 2);
            foreach(KeyValuePair<string, string> attribute in attributes) {
                switch(attribute.Key) {
                    case "prereq":  //pre-processed attributes
                    case "conseq":
                    case "set":
                    case "score":
                    break;
                    case "timerMax": //example attribute for a timed choice
                    if (!float.TryParse(attribute.Value, out this.timerMax))
                        Debug.LogError("Invalid scene timerMax argument "+attribute.Value);
                    break;
                    case "timerOption":
                    if (!UInt16.TryParse(attribute.Value, out this.timerOption))
                        Debug.LogError("Invalid scene timerOption argument "+attribute.Value);
                    break;
                    case "shake": //example attribute to make the camera shake for effects
                    if (!float.TryParse(attribute.Value, out this.shakeAmount))
                        Debug.LogError("Invalid scene shake argument "+attribute.Value);
                    break;
                    case "location": //example attribute that specifies a location (background, ambient sound, etc)
                    this.location = attribute.Value;
                    break;
                    case "sound": //example attribute that plays a sound
                    this.sound = attribute.Value;
                    break;
                    default:
                    Debug.LogWarning("Unknown scene attribute "+attribute.Key+" spotted.");
                    break;
                }
            }

            options = new List<Option>();
            List<string> elementLines = new List<string>();
            foreach(string line in rawLines.Skip(1+attributes.Count)) {
                int tabAmount = line.Count(f => f== '\t');
                if(tabAmount == 2) {
                    if(elementLines.Count > 0)
                        options.Add(new Option(story, elementLines));
                    elementLines = new List<string>();
                }
                elementLines.Add(line); // line is part of an option
            }
            if(elementLines.Count > 0)
                options.Add(new Option(story, elementLines));

        }

        public Option GetOption(ushort id) {
            return options[id];
        }

        public int GetOptionCount() {
            return options.Count;
        }

        public bool HasOptions() {
            return options!=null && options.Count > 0;
        }

        public float GetTimerMax() {
            return timerMax;
        }

        public ushort GetTimerOption() {
            return timerOption;
        }

        public string GetLocation() {
            return location;
        }

        public float GetShakeAmount() {
            return shakeAmount;
        }

        public string GetSound() {
            return sound;
        }

    }
}