using System;
using Xwt;
using System.Linq.Expressions;
using AlphaTab.Importer;
using System.IO;

namespace AlphaTab.Samples.XwtUi
{
	class MainWindow : Window
	{
		public static void Main (string [] args)
		{
			Application.Initialize (ToolkitType.Gtk);
			var window = new MainWindow ();
			window.Show ();
			Application.Run ();
		}

		public MainWindow ()
		{
			this.Closed += delegate { Application.Exit (); };

			var layout = new VBox ();
			this.MainMenu = BuildMenu ();
			var alphaTab = new AlphaTab.Platform.CSharp.XwtUi.AlphaTab ();
			var score = ScoreLoader.LoadScoreFromBytes (File.ReadAllBytes ("Canon.gp5"));
			alphaTab.Tracks = new [] { score.Tracks [0] };
			layout.PackStart (alphaTab, true);
			Content = layout;
		}

		public Menu BuildMenu ()
		{
			var menu = new Menu ();
			var quit = new MenuItem ("_Quit");
			quit.Clicked += delegate { this.Close (); };

			var file = new MenuItem ("_File");
			file.SubMenu = new Menu ();
			file.SubMenu.Items.Add (quit);

			menu.Items.Add (file);

			return menu;
		}
	}

}
