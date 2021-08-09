using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace StoryElements {

    // An option you can choose for a scene.
    public class Option : StoryElement {

        private string type;

        public Option(Story story, List<string> rawLines) : base(story, rawLines) {

            Dictionary<string,string> attributes = ProcessAttributes(rawLines, 3);
            foreach(KeyValuePair<string, string> attribute in attributes) {
                switch(attribute.Key) {
                    case "prereq": //pre-processed attributes
                    case "conseq":
                    case "set":
                    case "score":
                    break;
                    case "type": //option-specific attribute example
                    this.type = attribute.Value;
                    break;
                    default:
                    Debug.LogWarning("Unknown option attribute "+attribute.Key+" spotted.");
                    break;
                }
            }

        }

        public string GetButtonType() {
            return type;
        }

    }
}
