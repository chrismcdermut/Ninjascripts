#region Using declarations
using System;
using System.IO;
#endregion

namespace CustomHelpers.StreamWriter
{
    public class WriteTool
    {
        public void makeDirectory(string fileDirectoryPath){
            if (!Directory.Exists(fileDirectoryPath))
			{
			    Directory.CreateDirectory(fileDirectoryPath);
			}
        }

        // public void makeDirectory(fileDirectoryPath){
        //     if (!Directory.Exists(fileDirectoryPath))
		// 	{
		// 	    Directory.CreateDirectory(fileDirectoryPath);
		// 	}
        // }
    }
}
