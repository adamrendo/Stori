using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

namespace StoryElements {

    // Abstract class that is inherited by all story elements, which handles universal attribute processing and 
    public abstract class StoryElement {

        protected Story story;
        protected ushort id, targetId;
        protected string text;
        protected string[] prereqs, conseqs;
        protected Dictionary<string, string> overrides;
        protected int score;

        public StoryElement(Story story, List<string> rawLines) {

            this.story = story;
            string mainLine = rawLines[0];
            string[] splitMainLine = mainLine.Split(' ');

            if (UInt16.TryParse(splitMainLine[0], out this.id))
                mainLine = mainLine.Substring(mainLine.IndexOf(' ')+1);

            if (UInt16.TryParse(splitMainLine[splitMainLine.Length-1], out this.targetId))
                mainLine = mainLine.Substring(0, mainLine.LastIndexOf(' ')-1);

            this.text = mainLine.Trim('\t');
            
        }

        public Dictionary<string,string> ProcessAttributes(List<string> rawLines, ushort tabsRequired) {

            Dictionary<string,string> attributes = new Dictionary<string,string>();
            foreach(string line in rawLines.Skip(1)) {
                int tabAmount = line.Count(f => f== '\t');
                if(tabAmount == tabsRequired && line.Split(' ')[0].IndexOf(':') > 0) { // line is beginning of a sub-element or attribute
                    //line is an attribute (sub-elements musn't contain ':' in the first word)
                    string[] attribute = line.Trim('\t').Replace(": ",":").Split(':');
                    if(attribute.Length == 1) {
                        Debug.LogWarning("Undefined attribute " + attribute[0]+" detected.");
                        continue;
                    }
                    switch(attribute[0]) {
                        case "prereq":
                        prereqs = attribute[1].Split(',');
                        break;
                        case "conseq":
                        conseqs = attribute[1].Split(',');
                        break;
                        case "set":
                        if(overrides==null)
                            overrides = new Dictionary<string, string>();
                        overrides.Add(attribute[1].Split('=')[0], attribute[1].Split('=')[1]);
                        break;
                        case "score":
                        if (!int.TryParse(attribute[1], out this.score))
                            Debug.LogError("Invalid scene shake argument "+attribute[1]);
                        break;
                        default:
                        Debug.Log(String.Format("{0} attribute {1} spotted.",attribute[0], attribute[1]));
                        break;
                    }
                    attributes.Add(attribute[0], attribute[1]);
                }
            }
            return attributes;

        }

        public void OnLoad() {
            // Turns $variable$ into what it's supposed to be.
            foreach(KeyValuePair<string,string> variable in story.GetVariables()) {
                text = text.Replace("$"+variable.Key+"$", variable.Value);
            }
        }

        public static List<StoryElement> Partition(Story story, List<string> lines, ushort tabsRequired) {
            // I tried to make the partitioning of tabbed lines generalized for every story element so I wouldn't have to
            // repeat a lot of code but it didn't work. Perhaps you can figure it out. This does nothing and can be ignored.
            List<StoryElement> list = new List<StoryElement>();

            void AddProperElement(List<string> elementLines) {
                switch(tabsRequired) {
                    case 0: list.Add(new Chapter(story, elementLines)); break;
                    case 1: list.Add(new Scene(story, elementLines)); break;
                    case 2: list.Add(new Option(story, elementLines)); break;
                }
            }

            List<string> elementLines = new List<string>();
            foreach(string line in lines) {
                int tabAmount = line.Count(f => f== '\t');
                if(tabAmount == tabsRequired) { // line is beginning of a chapter
                    //you can add direction like character entrances and exits and location changes etc here. 
                    if(elementLines.Count > 0)
                        AddProperElement(elementLines);
                    elementLines = new List<string>();
                }
                elementLines.Add(line); // line is part of a chapter
            }
            AddProperElement(elementLines);

            return list;

        }
        public bool MatchesPrereqs(List<string> choices) {
            // Does the element match prerequisite with the given list of choices made?
            if(prereqs == null || prereqs.Length == 0)
                return true;
            foreach(string prereq in prereqs) {
                if(!choices.Contains(prereq))
                    return false;
            }
            return true;
        }

        public bool HasTarget() {
            return targetId != 0;
        }

        public ushort GetId() {
            return id;
        }

        public ushort GetTargetId() {
            return targetId;
        }

        public string GetText() {
            return text;
        }

        public string[] GetConseqs() {
            return conseqs;
        }

        public Dictionary<string,string> GetOverrides() {
            return overrides;
        }

        public string[] GetPrereqs() {
            return prereqs;
        }

        public int GetScore() {
            return score;
        }

    }

}