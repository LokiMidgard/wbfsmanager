namespace WBFSManager.Data
{
    //Sorts WbfsEntries in ascending order by name
    public class NameSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
        }
    }
    //Sorts WbfsEntries in ascending order by ID
    public class IdSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            int result = ((WbfsEntry)first).EntryID.CompareTo(((WbfsEntry)second).EntryID);
            if (result == 0)
                return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
            return result;
        }
    }
    //Sorts WbfsEntries in ascending order by size
    public class SizeSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            float firstFloat = ((WbfsEntry)first).EntrySize;
            float secondFloat = ((WbfsEntry)second).EntrySize;
            if (firstFloat < secondFloat)
                return -1;
            else if (firstFloat == secondFloat)
            {
                return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
            }
            return 1;
        }
    }
    //Sorts WbfsEntries in ascending order by index (locaiton on WBFS drive)
    public class IndexSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            int firstIndex = ((WbfsEntry)first).Index;
            float secondIndex = ((WbfsEntry)second).Index;
            if (firstIndex < secondIndex)
                return -1;
            else if (firstIndex == secondIndex)
            {
                return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
            }
            return 1;
        }
    }
    //Sorts WbfsEntries in ascending order by file path (location on local computer)
    public class FilePathSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            int result = ((WbfsEntry)first).FilePath.CompareTo(((WbfsEntry)second).FilePath);
            if (result == 0)
                return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
            return result;
        }
    }
    //Sorts WbfsEntries in ascending order by it's status
    public class StatusSorter : System.Collections.IComparer
    {
        public int Compare(object first, object second)
        {
            //order: NotYetCopied, Succeeded, Failed

            WbfsEntry.CopiedStates firstState = ((WbfsEntry)first).CopiedState;
            WbfsEntry.CopiedStates secondState = ((WbfsEntry)second).CopiedState;
            if (firstState == WbfsEntry.CopiedStates.Succeeded && secondState == WbfsEntry.CopiedStates.Failed)
                return 1;
            else if (firstState == WbfsEntry.CopiedStates.Succeeded && secondState == WbfsEntry.CopiedStates.NotYetCopied)
                return -1;
            else if (firstState == WbfsEntry.CopiedStates.NotYetCopied && secondState == WbfsEntry.CopiedStates.Failed)
                return 1;
            else if (firstState == WbfsEntry.CopiedStates.NotYetCopied && secondState == WbfsEntry.CopiedStates.Succeeded)
                return 1;
            else if (firstState == WbfsEntry.CopiedStates.Failed && secondState == WbfsEntry.CopiedStates.Succeeded)
                return -1;
            else if (firstState == WbfsEntry.CopiedStates.Failed && secondState == WbfsEntry.CopiedStates.NotYetCopied)
                return -1;

            else if (firstState == secondState)
            {
                return ((WbfsEntry)first).EntryName.CompareTo(((WbfsEntry)second).EntryName);
            }
            else return 0;
        }
    }
}