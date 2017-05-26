using System;

namespace WBFSManager.Data
{
    public class ErrorDetails
    {
        #region Fields
        /// <summary>
        /// Property representing the long message to be displayed (usually as the message in a messagebox)
        /// </summary>
        public String LongMessage { get; private set; }
        /// <summary>
        /// Property representing the long message to be displayed (usually as the title of a messagebox)
        /// </summary>
        public String ShortMessage { get; private set; }
        #endregion
        #region Constructors
        /// <summary>
        /// Paramterized constructor which sets the error messages.
        /// </summary>
        /// <param name="longMessage">The long message to be displayed (usually as the message in a messagebox).</param>
        /// <param name="shortMessage">The long message to be displayed (usually as the title of a messagebox).</param>
        public ErrorDetails(String longMessage, String shortMessage)
        {
            LongMessage = longMessage;
            ShortMessage = shortMessage;
        }
        #endregion
    }
}
