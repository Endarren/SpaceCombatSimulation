﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.src.Evolution
{
    public class ArenaRecord
    {
        private const char Delimiter = ';';
        public IEnumerable<string> Survivors;
        private string[] v;
        private IEnumerable<string> enumerable;

        public ArenaRecord(string recordLine)
        {
            var parts = recordLine.Split(Delimiter);
            
            Survivors = parts.ToList();
        }

        public ArenaRecord(IEnumerable<string> survivors)
        {
            Survivors = survivors;
        }

        public override string ToString()
        {
            return string.Join(";", Survivors.ToArray());
        }
    }
}
