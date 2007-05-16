// Don't give this the same version number as AWB please! What's the point of having per-assembly
// versions if we don't use them?
using System;
using System.Collections.Generic;
using System.Text;
using WikiFunctions.Plugin;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using WikiFunctions;
using WikiFunctions.Logging;
using WikiFunctions.Parse;
using WikiFunctions.Lists;
using System.Xml.Serialization;

namespace AutoWikiBrowser.Plugins.CFD
{
    public class CfdCore : IAWBPlugin
    {
        ToolStripMenuItem menuitem = new ToolStripMenuItem("Categories For Deletion plugin");
        ToolStripMenuItem aboutmenuitem = new ToolStripMenuItem("About the CFD plugin");

        ListMaker listMaker;

        CfdSettings Settings = new CfdSettings();

        public void Initialise(IAutoWikiBrowser MainForm)
        {
            listMaker = MainForm.ListMaker;

            //Set its check state to true
            // No, it should be checked when active and unchecked when not, and default to not!
            menuitem.CheckOnClick = true;
            menuitem.Checked = false;

            menuitem.Click += MenuItemClicked;
            aboutmenuitem.Click += AboutMenuItemClicked;

            //Make it change its checked state on click
            //menuitem.CheckOnClick = true;

            //Add it to the menu
            MainForm.PluginsToolStripMenuItem.DropDownItems.Add(menuitem);
            MainForm.HelpToolStripMenuItem.DropDownItems.Add(aboutmenuitem);
        }

        public string Name
        {
            get { return "Recategorize per CFD"; }
        }

        //This is the regex we will use, with the optional options of
        //ignoring case and compiled which is more efficient if we are going to re-use it repeatedly.
        Regex catRegex = new Regex("cat", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string ProcessArticle(IAutoWikiBrowser sender, ProcessArticleEventArgs ProcessArticleEventArgs)
        {
            //If menu item is not checked, then return
            if (!menuitem.Checked || Settings.Categories.Count == 0)
            {
                ProcessArticleEventArgs.Skip = false;
                return ProcessArticleEventArgs.ArticleText;
            }

            ProcessArticleEventArgs.EditSummary = "";
            string text = ProcessArticleEventArgs.ArticleText;

            Parsers parse = new Parsers();

            
            foreach (KeyValuePair<string, string> p in Settings.Categories)
            {
                bool noChange = true;

                if (p.Value == "")
                {
                    text = parse.RemoveCategory(p.Key, text, out noChange);
                    if(!noChange) ProcessArticleEventArgs.EditSummary += ", removed " + Variables.Namespaces[14] + p.Key;
                }
                else
                {
                    text = parse.ReCategoriser(p.Key, p.Value, text, out noChange);
                    if (!noChange) ProcessArticleEventArgs.EditSummary += ", replaced: " + Variables.Namespaces[14]
                         + p.Key + " → " + Variables.Namespaces[14] + p.Value;
                }
            }

            ProcessArticleEventArgs.Skip = (text == ProcessArticleEventArgs.ArticleText) && Settings.Skip;

            return text;
        }

        public void LoadSettings(object[] Prefs)
        {
            //Settings = (CfdSettings)Prefs[0];
            //menuitem.Checked = Settings.Enabled;
        }

        public object[] SaveSettings()
        {
            //Settings.Enabled = menuitem.Checked;

            //object[] Prefs = new object[1];
            //Prefs[0] = Settings;

            return null;//Prefs;
        }

        public void Reset()
        {
            //set default settings
            menuitem.Checked = true;
            Settings = new CfdSettings();
        }

        public void Nudge(out bool Cancel) { Cancel = false; }
        public void Nudged(int Nudges) { }

        private void MenuItemClicked(Object sender, EventArgs e)
        {
            CfdOptions o = new CfdOptions();

            o.Show(Settings, listMaker);

            menuitem.Checked = Settings.Enabled;

        }

        private void AboutMenuItemClicked(Object sender, EventArgs e)
        {
            try
            {
                AboutBox About = new AboutBox();
                About.Show();
            }
            catch { }
        }
    }

    [Serializable]
    public class CfdSettings
    {
        public bool Enabled = true;
        public Dictionary<string, string> Categories = new Dictionary<string, string>();
        public bool Skip = true;
    }

}

