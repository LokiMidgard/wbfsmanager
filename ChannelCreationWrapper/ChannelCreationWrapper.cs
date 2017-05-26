using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace ChannelCreationWrapper
{
    public static class ChannelCreationWrapper
    {
        /// <summary>
        /// Packs a wad given the fullpaths to the tik, tmd, cert, bnr and app files as well as the common-key.bin file.
        /// Re-signs the tik and tmd files. Replaes the title ID in tmd and tik with the specified, new title ID.
        /// Return codes:
        /// 0: Success
        /// -2: Could not find the cert file.
        /// -3: Could not find the opening banner file.
        /// -4: Could not find the TMD file.
        /// -5: Could not find the ticket file.
        /// -6: Error occurred while re-signing the ticket file.
        /// -7: Error occurred while decrypting title key.
        /// -8: Error reading an app file. (1-x)
        /// -9: Error opening target WAD file for writing.
        /// -10: Error, new Title ID is less than 4 letters
        /// -11: Error opening opening.bnr file.
        /// </summary>
        /// <param name="pathToOpeningBnr">Full path to the opening.bnr file.</param>
        /// <param name="pathToTicket">Full path to the .tik file.</param>
        /// <param name="pathToTMD">Full path to the .tmd file.</param>
        /// <param name="pathToCertFile">Full path to the .cert file.</param>
        /// <param name="pathToCommonKey">Full path to the common-key.bin file.</param>
        /// <param name="newTitleId">New 4 letter title ID</param>
        /// <param name="workingPath">Full path to the working folder with all the files including the remaining .app files. Must not end with \\</param>
        /// <param name="targetPath">Full path to the WAD file to create (including the name of the wad file and .wad extension)</param>
        /// <returns></returns>
        [DllImport("ChannelCreation.dll")]
        private static extern int packWad(StringBuilder pathToOpeningBnr, StringBuilder pathToTicket, StringBuilder pathToTMD, StringBuilder pathToCertFile, StringBuilder pathToCommonKey, StringBuilder newTitleId, StringBuilder workingPath, StringBuilder targetPath);
        /// <summary>
        /// Unpacks a wad given the path to it, the path to the common-key.bin file and the folder to unpack the files to.
        /// Note: After using this method change the working directory of the calling application back to it's initial value.
        /// Return codes:
        /// 0: Success
        /// -1: Cannot open input WAD file
        /// -2: Error reading WAD header
        /// -3: Error reading WAD header, secondary
        /// -4: Error, unknown header type
        /// -5: Error unexpected end of header file.
        /// -6: Error, WAD header too big
        /// -101: Bad header length
        /// -107: Error changing directory to ouput folder
        /// -108: Error reading cert, tik, tmd, app, or trailer file
        /// -109: Error opening cert file for writing.
        /// -110: Error opening trailer file for writing.
        /// -111: Error opening tmd file for writing.
        /// -112: Error opening tik file for writing.
        /// -201: Error opening key file
        /// -202: Error reading key file
        /// -203: Error opening app file for writing.
        /// -204: Error writing to app file.
        /// </summary>
        /// <param name="pathToWad">Full path to the WAD file to unpack.</param>
        /// <param name="pathToCommonKey">Full path to common-key.bin</param>
        /// <param name="trailerFilename">Full path to the .trailer file</param>
        /// <param name="outputFolder">Full path to the directory to store the unpacked files. Must not end in \\</param>
        /// <param name="certFilename">Full path to the cert file.</param>
        /// <param name="tikFilename">Full path to the .tik file.</param>
        /// <param name="tmdFilename">Full path to the .tmd file.</param>
        /// <returns></returns>
        [DllImport("ChannelCreation.dll")]
        private static extern int unpackWad(StringBuilder pathToWad, StringBuilder pathToCommonKey, StringBuilder certFilename, StringBuilder tikFilename, StringBuilder tmdFilename, StringBuilder trailerFilename, StringBuilder outputFolder);

        /// <summary>
        /// Extracts the opening.bnr file from an ISO image.
        /// Return codes:
        /// 0: Success
        /// -1: Error opening image file
        /// -2: Problem with the image, no partitions.
        /// -4: Opening.bnr not extracted.
        /// </summary>
        /// <param name="isoFileName"></param>
        /// <param name="pathToKey"></param>
        /// <param name="targetFilename"></param>
        /// <returns></returns>
        [DllImport("bannerExtractionWrapper.dll")]
        private static extern int ExtractOpeningBnr(StringBuilder isoFileName, StringBuilder pathToKey, StringBuilder targetFilename);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToIso"></param>
        /// <param name="pathToWad"></param>
        /// <param name="pathToBootDol">send String.Empty to use the 00000001.app from the wad</param>
        /// <param name="dolOrigDiscId">The disc id (6 letter) originally in the dol file (00000001.app) file</param>
        /// <param name="pathToCommonKey"></param>
        /// <param name="discId">The disc ID (6 letter) for the game to boot</param>
        /// <param name="newChanId">The 4 letter ID to use as the ID for the channel</param>
        /// <param name="outputFolder">Full path to output directory (can be a temp folder). Must not end with \\</param>
        /// <param name="outputWadFilename">Full path to output wad filename, including ".wad"</param>
        /// <returns></returns>
        public static int CreateChannel(String pathToIso, String pathToWad, String pathToBootDol, String dolOrigDiscId, String pathToCommonKey, String discId, String newChanId, String outputFolder, String outputWadFilename)
        {
            string currDir = Directory.GetCurrentDirectory();
            try
            {
                if (Directory.Exists(outputFolder))
                {
                    Directory.Delete(outputFolder, true);
                }
                Directory.CreateDirectory(outputFolder);
            }
            catch
            {
                return -306;
            }
            string pathTo01app = Path.Combine(outputFolder, "00000001.app");
            string pathTo00app = Path.Combine(outputFolder, "00000000.app");
            StringBuilder pathToCert = new StringBuilder(Path.Combine(outputFolder, discId + ".cert"));
            StringBuilder pathToTmd = new StringBuilder(Path.Combine(outputFolder, discId + ".tmd"));
            StringBuilder pathToTik = new StringBuilder(Path.Combine(outputFolder, discId + ".tik"));
            StringBuilder pathToTrailer = new StringBuilder(Path.Combine(outputFolder, discId + ".trailer"));
            int result = unpackWad(new StringBuilder(pathToWad), new StringBuilder(pathToCommonKey), pathToCert, pathToTik, pathToTmd, pathToTrailer, new StringBuilder(outputFolder));
            if ( result != 0)
                return result-500;
            
            //string pathToTmd = Path.Combine
            if (!File.Exists(pathTo00app) || !File.Exists(pathToCert.ToString()) || !File.Exists(pathToTmd.ToString()) || !File.Exists(pathToTik.ToString()) || !File.Exists(pathToTrailer.ToString()))
                return -301;      //Missing some unpacked files
            if (!File.Exists(pathTo01app))
                return -302;
            //If a dol file is specified, replace 00000001.app with the dol file.
            if (!pathToBootDol.Equals(String.Empty) && File.Exists(pathToBootDol))
            {
                File.Delete(pathTo01app);
                File.Copy(pathToBootDol, pathTo01app);
            }
            if (dolOrigDiscId.Length < 6 || discId.Length < 6)
                return -303;
            byte[] data = File.ReadAllBytes(pathTo01app);
            bool idReplaced = false;
            for (long i = 0; i < data.LongLength; i++)
            {
                if (data[i] == dolOrigDiscId[0] && data[i + 1] == dolOrigDiscId[1] && data[i + 2] == dolOrigDiscId[2] && data[i + 3] == dolOrigDiscId[3] && data[i + 4] == dolOrigDiscId[4] && data[i + 5] == dolOrigDiscId[5])
                {
                    byte[] bytesDiscId = Encoding.ASCII.GetBytes(discId);
                    data[i] = bytesDiscId[0];
                    data[i + 1] = bytesDiscId[1];
                    data[i + 2] = bytesDiscId[2];
                    data[i + 3] = bytesDiscId[3];
                    data[i + 4] = bytesDiscId[4];
                    data[i + 5] = bytesDiscId[5];
                    idReplaced = true;
                    break;
                }
            }
            if (!idReplaced)
                return -304;
            File.Delete(pathTo01app);
            File.WriteAllBytes(pathTo01app, data);
            File.Delete(pathTo00app);//, Path.Combine(outputFolder, "XX0.X"));
            StringBuilder bannerFile = new StringBuilder(Path.Combine(outputFolder, "opening.bnr"));
            Directory.SetCurrentDirectory(currDir);
            result=1;
            result = ExtractOpeningBnr(new StringBuilder(pathToIso), new StringBuilder(pathToCommonKey), bannerFile);
            if (result != 0)
            {
                return result-600;
            }
            if (!File.Exists(bannerFile.ToString()))
                return -305;
            Directory.SetCurrentDirectory(currDir);
            result=1;
            result = packWad(bannerFile, pathToTik, pathToTmd, pathToCert, new StringBuilder(pathToCommonKey), new StringBuilder(newChanId), new StringBuilder(outputFolder), new StringBuilder(outputWadFilename));
            if (result != 0)
            {
                return result-700;
            }
            return 0;
        }
    }
}
