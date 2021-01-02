// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal sealed class Format0SubTable : CMapSubTable
    {
        public Format0SubTable(ushort language, PlatformIDs platform, ushort encoding, byte[] glyphIds)
            : base(platform, encoding, 0)
        {
            this.Language = language;
            this.GlyphIds = glyphIds;
        }

        public ushort Language { get; }

        public byte[] GlyphIds { get; }

        public override bool TryGetGlyphId(int codePoint, out ushort glyphId)
        {
            uint b = (uint)codePoint;
            if (b >= this.GlyphIds.Length)
            {
                glyphId = 0;
                return false;
            }

            glyphId = this.GlyphIds[b];
            return true;
        }

        public static IEnumerable<Format0SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
        {
            // format has already been read by this point skip it
            ushort length = reader.ReadUInt16();
            ushort language = reader.ReadUInt16();
            int glyphsCount = length - 6;

            // char 'A' == 65 thus glyph = glyphIds[65];
            byte[] glyphIds = reader.ReadBytes(glyphsCount);

            foreach (EncodingRecord encoding in encodings)
            {
                yield return new Format0SubTable(language, encoding.PlatformID, encoding.EncodingID, glyphIds);
            }
        }
    }
}
