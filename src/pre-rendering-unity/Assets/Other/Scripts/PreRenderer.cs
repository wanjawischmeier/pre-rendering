using System.IO;
using UnityEngine;
using UnityEditor;

namespace PreRendering
{
    public class Map2
    {
        public string videoPath;
        public string filename;
        public int[] fileData;
        public Map2(string _videoPath)
        {
            videoPath = _videoPath;

            if (videoPath.Contains("_["))
            {
                fileData = new int[5];
                string raw =        Path.GetFileNameWithoutExtension(videoPath);
                filename =          raw.Split('_')[0];
                string rawData =    raw.Split('_')[1];

                rawData = rawData.Trim('[');
                rawData = rawData.Trim(']');
                string[] rawDataList = new string[5];
                rawDataList = rawData.Split(';');

                for (int i = 0; i < rawDataList.Length; i++)
                {
                    fileData[i] = int.Parse(rawDataList[i]);
                }
            }
        }


        public bool Create(string _filename, int _framecount, int _axis, int _lengthSteps, int _lengthX, int _lengthY, string _mask = "{0}_[{1};{2};{3};{4};{5}]")
        {
            if (File.Exists(videoPath))
            {
                string filename = string.Format(_mask, _filename, _framecount, _axis, _lengthSteps, _lengthX, _lengthY);
                filename = string.Format("{0}/{1}", Path.GetDirectoryName(filename), Path.GetFileName(filename));

                try
                {
                    //AssetDatabase.RenameAsset(videoPath, filename);
                    return true;
                }

                catch
                {
                    return false;
                }
            }

            else
            {
                return false;
            }
        }
    }




    public class PreRenderer
    {
        public Map2 map;
        int frame;
        public int currentFrame
        {
            get { return frame; }
        }
        public Vector2Int currentCoordinates
        {
            get { return coordinates; }

            set
            {
                lastCoordinates = coordinates;

                if (coordinates.x <= map.fileData[3] && coordinates.x >= 0)
                {
                    coordinates.x = value.x;
                } else if (coordinates.x < 0)
                {
                    coordinates.x = 0;
                } else
                {
                    coordinates.x = map.fileData[3];
                }

                if (coordinates.y <= map.fileData[4] && coordinates.y >= 0)
                {
                    coordinates.y = value.y;
                } else if (coordinates.y < 0)
                {
                    coordinates.y = 0;
                } else
                {
                    coordinates.y = map.fileData[4];
                }
            }
        }
        public Vector2Int currentDirection
        {
            get
            {
                Vector2Int direction = new Vector2Int(0, 0);

                if (currentCoordinates.x > lastCoordinates.x)
                {
                    direction.x = 1;
                }

                else if (currentCoordinates.x < lastCoordinates.x)
                {
                    direction.x = -1;
                }


                if (currentCoordinates.y > lastCoordinates.y)
                {
                    direction.y = 1;
                }

                else if (currentCoordinates.y < lastCoordinates.y)
                {
                    direction.y = -1;
                }

                return direction;
            }
        }

        Vector2Int lastCoordinates;
        Vector2Int coordinates;
        public PreRenderer(Map2 _map)
        {
            map = _map;
            frame = 0;
            currentCoordinates = Vector2Int.zero;
        }
    }
}

