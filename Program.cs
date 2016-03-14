using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography; //this is here cause I plan on adding CRC32 support eventually

namespace OggFileReader
{
    class Program
    {

        static void Main(string[] args)
        {


            byte[] capture_pattern = new byte[] { 0x4f, 0x67, 0x67, 0x53 }; // capture pattern "OggS"

            int patternamountfound = 0;
            int beginfound = 0;
            int continuefound = 0;
            int endfound = 0;
            int unsetfound = 0;
            int unknownfound = 0;
            int filecount = 0;
            int currentpage = 0; //not used yet
            byte[] granular = new byte[8];
            byte[] streamserial = new byte[4];
            byte[] tempserial = new byte[4];
            bool showoffsets = false;
            bool mappages = false;
            var filelist = new List<string>();
            var offsets = new List<long>();
            string filename = "";
            List<string> outputlog = new List<string>();


            if (args.Length == 0) //needs at least 1 arg, if not, let the user know
            {
                Console.WriteLine("No Args set.");
            }
            else
                foreach (string arg in args) //process args individually
                {
                    if (arg.EndsWith(".ogg"))
                    {
                        filename = arg.ToString();
                        filelist.Add(filename);
                        filecount++;
                    }
                    if (arg.StartsWith("-o") || arg.StartsWith("/o"))
                    {
                        Console.WriteLine("Showing offsets.");
                        showoffsets = true;
                    }
                    if(arg.StartsWith("-m") || arg.StartsWith("/m"))
                    {
                        Console.WriteLine("Mapping page layout to console.");
                        mappages = true;
                    }
                    if (arg.StartsWith("-h") || arg.StartsWith("/h") || arg.StartsWith("-?") || arg.StartsWith("/?"))
                    {
                        Console.WriteLine("-h/h-?/? show this help message.");
                        Console.WriteLine("oggfilereader.exe filepath <options>");
                        Console.WriteLine("-o or /o to show offets (in decimal) at which a frame is found and will display warnings on unknown frames, can be used with -m /m but will not look pretty.");
                        Console.WriteLine("-m or /m map the pages on screen as they are found, can be used with -o /o but will not look pretty.");
                    }

                }
            if (filename == "") //no filename/path specified
            {
                Console.WriteLine("No Filename specified.");
                Console.WriteLine("Drop a file on the EXE or include a pathname as an argument.");


            }
            else if(!File.Exists(filename))
            {
                Console.WriteLine("File not found.");


            }
            else // on to the fun stuff!
            {
                using (FileStream filestream = new FileStream(filename, FileMode.Open)) //open the file
                {
                    filestream.Seek(0, SeekOrigin.Begin);
                    while (filestream.Position != filestream.Length) //keep looping till we hit the EoF
                    {
                        if (filestream.ReadByte() == capture_pattern[0]) // check for first byte (0x4f) in capture pattern
                        {
                            if (filestream.ReadByte() == capture_pattern[1]) //if 0x4f is found, look for 0x67
                            {
                                if (filestream.ReadByte() == capture_pattern[2]) // look for 0x67 again
                                {
                                    if (filestream.ReadByte() == capture_pattern[3]) //is 0x57 found? if so we have a Ogg page header on our hands.
                                    {
                                        offsets.Add(filestream.Position - 4); // add the offset of this page to the list
                                        patternamountfound++; // increase overall page count by 1
                                        if (showoffsets) // if the arg for showing offsets is set, list off the offsets as they are found
                                        {
                                            Console.WriteLine(filestream.Position - 4);
                                        }
                                        if (filestream.ReadByte() == 0x00) //check if version byte is 0x00(??), no fail state for this yet as current ogg container is ver 0
                                        {

                                            switch (filestream.ReadByte()) // this switch determines what kind of page we found
                                            {
                                                case 0x00:
                                                    unsetfound++; //found a type 00 page, so increment the counter
                                                    if(mappages == true)
                                                    {
                                                        Console.Write("0");
                                                    }
                                                    filestream.Position += 8;
                                                    for (int i = 0; i < 3; i++)
                                                    {
                                                        tempserial[i] = (byte)filestream.ReadByte(); // read the page serial to compare with page type 02 serial
                                                    }


                                                    if (BitConverter.ToString(tempserial) != BitConverter.ToString(streamserial))
                                                    {
                                                        Console.WriteLine("Found different serial, is this a chained ogg?"); // this serial is different, chained ogg maybe?
                                                    }

                                                    break;
                                                case 0x01:
                                                    continuefound++; // page type 01 found, increment and move on, maybe add serial compare?
                                                    if(mappages == true)
                                                    {
                                                        Console.Write("1");
                                                    }
                                                    break;
                                                case 0x02:
                                                    beginfound++; // page type 02 found, increment and do initial stream serial parse.
                                                    if(mappages == true)
                                                    {
                                                        Console.Write("2");
                                                    }
                                                    filestream.Position += 8;
                                                    for (int i = 0; i < 3; i++)
                                                    {
                                                        streamserial[i] = (byte)filestream.ReadByte();
                                                    }
                                                    break;
                                                case 0x04: //page type 04 found, increment counter and parse final granular position for total length.
                                                    endfound++;
                                                    if(mappages == true)
                                                    {
                                                        Console.Write("4");
                                                    }
                                                    for (int i = 0; i < 7; i++)
                                                    {
                                                        granular[i] = (byte)filestream.ReadByte();
                                                    }
                                                    break;
                                                default: //no known type is detected
                                                    //Console.WriteLine("Unknown frame type detected at {0}.", filestream.Position - 1);                                                 
                                                    unknownfound++;
                                                    if(showoffsets ==true)
                                                    {
                                                        Console.WriteLine("Uknown frame type detected at {0}.", offsets[offsets.Count - 1]);
                                                    }
                                                    if(mappages == true)
                                                    {
                                                        filestream.Position -= 1;
                                                        Console.Write(filestream.ReadByte().ToString());
                                                    }
                                                    break;

                                            }

                                        }
                                    }
                                    else
                                    {
                                        filestream.Position -= 1; // 2nd 0x67 found but not the final 0x53, push back position and start over
                                    }
                                }
                                else
                                {
                                    filestream.Position -= 1; //found 0x67 once, but not a 2nd time, push position back and start over at 0x4f
                                }
                            }
                            else
                            {
                                filestream.Position -= 1; // found 0x4f but not 0x67 so push back the stream position to check for 0x4f again
                            }
                        } // no byte match for 0x4f so go back to the start.
                    }
                }
                if (!BitConverter.IsLittleEndian) //check endianness of the host machine, adjust byte order accordingly
                {
                    Array.Reverse(granular); // output granular in reverse byte order if not little endian
                }
                Console.Write("\n");
                Console.WriteLine("Number of files processed: {0}", filelist.Count); // number of files processed, multi-file not yet supported
                Console.WriteLine("Steam Serial Number: {0}", BitConverter.ToString(streamserial, 0)); // output stream serial
                Console.WriteLine("Total Samples : {0}", BitConverter.ToInt64(granular, 0)); //output total samples using granular value from 04 type frame
                Console.WriteLine("Number of Pages found: {0}", patternamountfound); // total number of ogg pages found
                Console.WriteLine("Number of Begin Pages found: {0}", beginfound); // type 02 pages found
                Console.WriteLine("Number of Continued Pages found: {0}", continuefound); //type 01 pages found
                Console.WriteLine("Number of End Pages found: {0}", endfound); // type 04 pages found
                Console.WriteLine("Number of Unset Pages found: {0}", unsetfound); // type 00 pages found
                Console.WriteLine("Number of Unknown Pages found: {0}", unknownfound); // pages of unknown types (not 00 01 02 or 04)
                if (beginfound == 0) //no beginning page found, file may be corrupt or made incorrectly
                {
                    Console.WriteLine("Missing Beginning Page. Damaged file?");
                }
                if (unknownfound > 0) //found some unknown pages, file may be corrupt or made wrong.
                {
                    Console.WriteLine("Found some unknown page types. File damaged or Corrupt?");
                }
                if (patternamountfound == beginfound + continuefound + endfound + unsetfound + unknownfound)
                {
                    Console.WriteLine("Page count looks good."); // page counts add up, everything looks good.
                }
                else
                {
                    Console.WriteLine("Page count invalid or missing frames."); //page count is off, we might be missing frames
                }

            }
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(); //wait for keypress to exit.

        }
    }
}
