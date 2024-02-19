using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace ConnectedComponentLabeling
{
    public class CCLBlobDetector
    {
        private List<List<int>>  _blobs = new List<List<int>>(10);
        private List<int> _currentBlobList = new List<int>(); 

        private bool[] _labeledPixels;
        private bool[] _pixelsForeground;

        public CCLBlobDetector fallback;

        private int _bmpWidth;
        private int _bmpHeight;

        private bool completeFallback;

        public int Initialize(Texture2D bmp)
        {
            _bmpWidth = bmp.width;
            _bmpHeight = bmp.height;

            _pixelsForeground = new bool[bmp.width * bmp.height];
            _labeledPixels = new bool[bmp.width * bmp.height];

            for (int row = 0; row < bmp.height; row++)
            {
                for (int col = 0; col < bmp.width; col++)
                {
                    SetPixel(col, row, bmp.GetPixel(col, row).a > 0);
                }
            }

            fallback = this;

            return bmp.width * bmp.height;
        }

        public void ResetPixels()
        {
            Array.Clear(_pixelsForeground, 0, _pixelsForeground.Length);
        }

        public void SetPixel(int x, int y, bool set)
        {
            var pixelId = GetPixelId(x, y);
            _pixelsForeground[pixelId] = set;
        }

        public void SetPixel(int i, bool set)
        {
            _pixelsForeground[i] = set;
        }

        /// <summary>
        /// Returns a collection with lists. Each list contains a blob that is represented by all its pixelIds.
        /// </summary>
        /// <returns></returns>
        public List<List<int>> GetBlobs()
        {
            var pixelIds = new Stack<int>();
            int nrPixel = _bmpWidth*_bmpHeight;

            _labeledPixels = new bool[nrPixel];
            _blobs.Clear();
            _currentBlobList = new List<int>();

            for (int i = 0; i < nrPixel; i++)
            {
                bool anythingLabeled = false;
                if (IsPixelForeground(i) && !IsPixelLabeled(i))
                {
                    anythingLabeled = true;
                    AddPixelToCurrentBlob(i);
                    pixelIds.Push(i);
                }

                while (pixelIds.Any())
                {
                    var nextPixel = pixelIds.Pop();
                    var neighbours = GetNeighboursBy4Connectivity(nextPixel).ToList();
                    foreach (var neighbour in neighbours)
                    {
                        if (IsPixelForeground(neighbour) && !IsPixelLabeled(neighbour))
                        {
                            AddPixelToCurrentBlob(neighbour);
                            pixelIds.Push(neighbour);
                        }
                    }
                }

                if (anythingLabeled)
                {
                    _currentBlobList.Sort(); // Sort for playback?
                    _blobs.Add(_currentBlobList);
                    _currentBlobList = new List<int>();
                }
            }

            return _blobs;
        }

         public List<int> GetBlob(int start)
        {
            completeFallback = true;

            var pixelIds = new Stack<int>();
            int nrPixel = _bmpWidth*_bmpHeight;

            _labeledPixels = new bool[nrPixel];
            _blobs.Clear();
            _currentBlobList = new List<int>();

            int i = start;

            if (IsPixelForeground(i) && !IsPixelLabeled(i))
            {
                AddPixelToCurrentBlob(i);
                pixelIds.Push(i);
            }

            while (pixelIds.Any())
            {
                var nextPixel = pixelIds.Pop();
                var neighbours = GetNeighboursBy4Connectivity(nextPixel).ToList();
                foreach (var neighbour in neighbours)
                {
                    if (IsPixelForeground(neighbour) && !IsPixelLabeled(neighbour))
                    {
                        AddPixelToCurrentBlob(neighbour);
                        pixelIds.Push(neighbour);
                    }
                }
            }

            if (completeFallback) return new List<int>();
            // _currentBlobList.Sort();
            return _currentBlobList;
        }

        private int GetPixelId(int col, int row)
        {
            return col + row * _bmpWidth;
        }

        private bool IsPixelLabeled(int pixelId)
        {
            return _labeledPixels[pixelId];
        }

        private IEnumerable<int> GetNeighboursBy4Connectivity(int pixelId)
        {
            int col = GetColumn(pixelId);
            int row = GetRow(pixelId);

            int? leftId = GetPixelIdValidated(col - 1, row);
            int? rightId = GetPixelIdValidated(col + 1, row);
            int? topId = GetPixelIdValidated(col, row + 1);
            int? bottomId = GetPixelIdValidated(col, row - 1);

            var validNeighbours = new List<int>();
            if (rightId.HasValue)
            {
                validNeighbours.Add(rightId.Value);
            }
            if (leftId.HasValue)
            {
                validNeighbours.Add(leftId.Value);
            }
            if (topId.HasValue)
            {
                validNeighbours.Add(topId.Value);
            }
            if (bottomId.HasValue)
            {
                validNeighbours.Add(bottomId.Value);
            }

            return validNeighbours;
        }

        private void AddPixelToCurrentBlob(int pixelId)
        {
            _labeledPixels[pixelId] = true;

            _currentBlobList.Add(pixelId);
        }

        private int GetColumn(int pixelId)
        {
            return pixelId%_bmpWidth;
        }

        private int GetRow(int pixelId)
        {
            return pixelId/_bmpWidth;
        }

        private bool IsPixelForeground(int pixelId)
        {
            completeFallback = completeFallback && !_pixelsForeground[pixelId];
            return _pixelsForeground[pixelId] || fallback._pixelsForeground[pixelId];
        }

        private int? GetPixelIdValidated(int col, int row)
        {
            if (col < 0 || col >= _bmpWidth || row < 0 || row >= _bmpHeight)
                return null;

            int id = col + row*_bmpWidth;
            return id;
        }
    }
}