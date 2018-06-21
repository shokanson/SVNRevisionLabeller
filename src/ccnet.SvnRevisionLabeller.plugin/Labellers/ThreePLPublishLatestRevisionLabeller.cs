using Exortech.NetReflector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Util;

namespace CcNet.Labeller
{
    /// <summary>
    /// Generates CC.NET label numbers using the Microsoft-recommended versioning 
    /// format (ie. Major.Minor.Build.Revision). The build number is auto-
    /// incremented for each successful build, and the latest Subversion commit number
    /// is used to generate the revision. The resultant label is accessible from 
    /// apps such as MSBuild via the <c>$(CCNetLabel)</c> property , and NAnt via 
    /// the <c>${CCNetLabel}</c> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class was inspired by Jonathan Malek's post on his blog 
    /// (<a href="http://www.jonathanmalek.com/blog/CruiseControlNETAndSubversionRevisionNumbersUsingNAnt.aspx">CruiseControl.NET and Subversion Revision Numbers using NAnt</a>),
    /// which used NAnt together with Subversion to retrieve the latest revision number. This plug-in moves it up into 
    /// CruiseControl.NET itself, so that you can see the latest revision number appearing in CCTray. 
    /// </para>
    /// <para>
    /// The plugin was then substantially rewritten by fezguy (http://code.google.com/u/fezguy/), incorporating
    /// the following new features:
    /// <ul>
    /// <li>defaults to use the Microsoft recommended versioning format;</li>
    /// <li>option to increment the build number always, similar to DefaultLabeller [default: false];</li>
    /// <li>option to reset the build number to 0 after a (major/minor) version change [default: true];</li>
    /// <li>option to use "--trust-server-cert" command line parameter (Subversion v1.6+)</li>
    /// <li>"pattern" property to support user-defined build number format;</li>
    /// <li>handles an additional "rebuild" field via "pattern" property which counts builds of same revision;</li>
    /// <li>option to include a postfix on version;</li>
    /// <li>handles the quoting of Subversion URLs with spaces</li>
    /// </ul>
    /// </para>
    /// </remarks>
	[ReflectorType("3PLPublishLatestRevisionLabeller")]
    public class ThreePLPublishLatestRevisionLabeller : ILabeller
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreePLPublishLatestRevisionLabeller"/> class.
        /// </summary>
        public ThreePLPublishLatestRevisionLabeller() { }

        #endregion

        #region Properties
        /// <summary>
        /// Path to the AssemblyInfo.cs file that has the Major/Minor specified in an AssemblyVersion attribute.
        /// If specified, overrides the Major/Minor properties
        /// </summary>
        [ReflectorProperty("publishPath", Required = false)]
        public string PublishPath { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Runs the task, given the specified <see cref="IIntegrationResult"/>, in the specified <see cref="IProject"/>.
        /// </summary>
        /// <param name="result">The label for the current build.</param>
        public void Run(IIntegrationResult result)
        {
            result.Label = Generate(result);
        }

		private struct LatestFile
		{
			public string Name;
			public string Find;
			public string Label;
			public int Index;
		}

		private List<LatestFile> NeededFiles = new List<LatestFile> {
			new LatestFile { Name = "latest-wms.txt", Label = "wms", Find = "1.0.0", Index = 0 },
			new LatestFile { Name = "latest-smartui.txt", Label = "ui", Find = "-", Index = 1 },
			new LatestFile { Name = "latest-scannerapp-master.txt", Label = "scan", Find = "-", Index = 1  }
		};

        /// <summary>
        /// Returns the label to use for the current build.
        /// </summary>
        /// <param name="resultFromLastBuild">IntegrationResult from last build used to determine the next label.</param>
        /// <returns>The label for the current build.</returns>
        /// <exception cref="System.ArgumentException">Thrown when an error occurs while formatting the version number using the various formatting tokens.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when an error occurs while formatting the version number and an argument has not been specified.</exception>
        public string Generate(IIntegrationResult resultFromLastBuild)
        {
			Log.Trace("ThreePLPublishLatestRevisionLabeller running");

			if (string.IsNullOrEmpty(PublishPath)) throw new ArgumentException("must set the <publishPath> value in the labeller configuration");
			if (!Directory.Exists(PublishPath)) throw new ArgumentException("must set the <publishPath> value to an existing folder");

			StringBuilder label = new StringBuilder();

			NeededFiles.ForEach(latest => {
				string latestFilePath = Path.Combine(PublishPath, latest.Name);
				if (!File.Exists(latestFilePath)) throw new ArgumentException(string.Format("must set the <publishPath> value to a folder that contains '{0}'", latest.Name));

				string latestPath = File.ReadAllLines(latestFilePath).FirstOrDefault();
				if (string.IsNullOrEmpty(latestPath)) throw new ArgumentException(string.Format("'{0}' does not contiain any data", latest.Name));
				latestPath = Path.Combine(PublishPath, latestPath);
				if (!Directory.Exists(latestPath)) throw new ArgumentException(string.Format("'{0}' specifies a non-existent folder: '{1}'", latest.Name, latestPath));

				if (label.Length > 0) label.Append('-');
				label.AppendFormat("{0}:{1}", latest.Label, latestPath.Substring(latestPath.IndexOf(latest.Find)+latest.Index));
			});

            Log.Trace("Label = {0}", label.ToString());
            return label.ToString();
        }

        #endregion
    }
}