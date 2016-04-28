﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stegosaurus.Extensions.InputExtensions
{
    public class ContentType : IInputType
    {
        public string FilePath { get; set; }

        public ContentType(string _filePath)
        {
            FilePath = _filePath;
        }
    }
}
