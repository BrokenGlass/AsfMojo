using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsfMojo.Parsing;

namespace AsfMojo.File
{
    /// <summary>
    /// Fluent interface to update properties in an ASF file
    /// </summary>
    public interface IAsfFileUpdateOptions
    {
        IAsfFileUpdateOptions WithFileCreationTime(DateTime fileCreationTime);
        IAsfFileUpdateOptions WithAuthor(string author);
        IAsfFileUpdateOptions WithDescription(string description);
        IAsfFileUpdateOptions WithTitle(string title);
        IAsfFileUpdateOptions WithRating(string rating);
        IAsfFileUpdateOptions WithCopyright(string copyright);
        void Update(string targetFileName = null);
    }

    /// <summary>
    /// Fluent interface to update properties in an ASF file
    /// </summary>
    internal class AsfFileUpdateOptions : IAsfFileUpdateOptions
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Copyright { get; set; }
        public string Description { get; set; }
        public string Rating { get; set; }
        public DateTime? FileCreationTime { get; set; }

        internal AsfFileUpdateOptions()
        {

        }

        public IAsfFileUpdateOptions WithFileCreationTime(DateTime fileCreationTime)
        {
            FileCreationTime = fileCreationTime;
            return this;
        }

        public IAsfFileUpdateOptions WithAuthor(string author)
        {
            Author = author;
            return this;
        }

        public IAsfFileUpdateOptions WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public IAsfFileUpdateOptions WithTitle(string title)
        {
            Title = title;
            return this;
        }

        public IAsfFileUpdateOptions WithRating(string rating)
        {
            Rating = rating;
            return this;
        }

        public IAsfFileUpdateOptions WithCopyright(string copyright)
        {
            Copyright = copyright;
            return this;
        }

        public void Update(string targetFileName = null)
        {
            AsfFile asfFile = new AsfFile(FileName);

            if (FileCreationTime != null)
            {
                var asfFileProperties = asfFile.GetAsfObject<AsfFileProperties>();
                asfFileProperties.CreationTime = FileCreationTime.Value;
            }

            var asfContentDescription = asfFile.GetAsfObject<AsfContentDescriptionObject>();

            if(Title!=null)
                asfContentDescription.ContentProperties["Title"] = Title;
            if (Author != null)
                asfContentDescription.ContentProperties["Author"] = Author;
            if (Copyright != null)
                asfContentDescription.ContentProperties["Copyright"] = Copyright;
            if (Description != null)
                asfContentDescription.ContentProperties["Description"] = Description;
            if (Rating != null)
                asfContentDescription.ContentProperties["Rating"] = Rating;

            asfFile.Update(targetFileName);
        }
    }
}
