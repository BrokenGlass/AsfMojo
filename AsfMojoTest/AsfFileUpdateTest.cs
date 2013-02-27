using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using AsfMojo.Parsing;
using System.IO;
using AsfMojo.File;

namespace AsfMojoTest
{
    /// <summary>
    /// Summary description for AsfUpdateTest
    /// </summary>
    [TestClass]
    public class AsfFileUpdateTest
    {

        private string testVideoFileName = ConfigurationManager.AppSettings["VideoFile"];
        private string testAudioFileName = ConfigurationManager.AppSettings["AudioFile"];

        [TestMethod]
        public void UpdateContentDescriptionProperties()
        {
            string testBackupFileName = "testContentDescriptionUpdate.wmv";

            AsfFile asfFile = new AsfFile(testVideoFileName);
            AsfContentDescriptionObject contentDescription = asfFile.GetAsfObject<AsfContentDescriptionObject>();
            contentDescription.ContentProperties["Author"] = "Fred Fish";
            contentDescription.ContentProperties["Copyright"] = "Copyright (c) 2011";
            contentDescription.ContentProperties["Title"] = "Some title";
            contentDescription.ContentProperties["Description"] = "Some lengthy description of the content";
            contentDescription.ContentProperties["Rating"] = "5.0";

            asfFile.Update(testBackupFileName);

            asfFile = new AsfFile(testBackupFileName);
            contentDescription = asfFile.GetAsfObject<AsfContentDescriptionObject>();

            Assert.AreEqual(contentDescription.ContentProperties["Author"], "Fred Fish");
            Assert.AreEqual(contentDescription.ContentProperties["Copyright"], "Copyright (c) 2011");
            Assert.AreEqual(contentDescription.ContentProperties["Title"], "Some title");
            Assert.AreEqual(contentDescription.ContentProperties["Description"], "Some lengthy description of the content");
            Assert.AreEqual(contentDescription.ContentProperties["Rating"], "5.0");

            File.Delete(testBackupFileName);
        }

        [TestMethod]
        public void UpdateContentDescriptionPropertiesFluent()
        {
            string testBackupFileName = "testContentDescriptionUpdate.wmv";

            AsfFile.From(testVideoFileName)
                   .WithFileCreationTime(DateTime.Parse("2/27/2011"))
                   .WithAuthor("Fred Fish")
                   .WithDescription("Some lengthy description of the content")
                   .WithCopyright("Copyright (c) 2011")
                   .WithTitle("Some title")
                   .WithRating("5.0")
                   .Update(testBackupFileName);

            AsfFile asfFile = new AsfFile(testBackupFileName);

            var asfFileProperties = asfFile.GetAsfObject<AsfFileProperties>();
            Assert.AreEqual(asfFileProperties.CreationTime, DateTime.Parse("2/27/2011"));

            AsfContentDescriptionObject contentDescription = asfFile.GetAsfObject<AsfContentDescriptionObject>();
            Assert.AreEqual(contentDescription.ContentProperties["Author"], "Fred Fish");
            Assert.AreEqual(contentDescription.ContentProperties["Copyright"], "Copyright (c) 2011");
            Assert.AreEqual(contentDescription.ContentProperties["Title"], "Some title");
            Assert.AreEqual(contentDescription.ContentProperties["Description"], "Some lengthy description of the content");
            Assert.AreEqual(contentDescription.ContentProperties["Rating"], "5.0");

            File.Delete(testBackupFileName);
        }
    }
}
