using System;

namespace WBFSManager.Data
{
    public class ChannelPreset
    {
        public const string DESCRIPTIONELEMENTNAME = "Description";
        public const string DOLFILENAMEELEMENTNAME = "DolFilename";
        public const string ORIGINALTITLEIDELEMENTNAME = "OriginalTitleId";
        public String Description { get; set; }
        public String DolFilePath { get; set; }
        public String OriginalTitleID { get; set; }
        public ChannelPreset(String description, String dolFilePath, String originalTitleId)
        {
            Description = description;
            DolFilePath = dolFilePath;
            OriginalTitleID = originalTitleId;
        }
    }
}
