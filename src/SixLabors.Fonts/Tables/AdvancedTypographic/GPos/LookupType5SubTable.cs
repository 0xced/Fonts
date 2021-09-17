// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Mark-to-Ligature Attachment Positioning Subtable.
    /// The MarkToLigature attachment (MarkLigPos) subtable is used to position combining mark glyphs with respect to ligature base glyphs.
    /// With MarkToBase attachment, described previously, each base glyph has an attachment point defined for each class of marks.
    /// MarkToLigature attachment is similar, except that each ligature glyph is defined to have multiple components (in a virtual sense — not actual glyphs),
    /// and each component has a separate set of attachment points defined for the different mark classes.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable"/>
    /// </summary>
    internal sealed class LookupType5SubTable
    {
        internal LookupType5SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort subTableFormat = reader.ReadUInt16();

            return subTableFormat switch
            {
                1 => LookupType5Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {subTableFormat}. Should be '1'."),
            };
        }

        internal sealed class LookupType5Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable markCoverage;
            private readonly CoverageTable ligatureCoverage;
            private readonly MarkArrayTable markArrayTable;
            private readonly LigatureArrayTable ligatureArrayTable;

            public LookupType5Format1SubTable(CoverageTable markCoverage, CoverageTable ligatureCoverage, MarkArrayTable markArrayTable, LigatureArrayTable ligatureArrayTable)
            {
                this.markCoverage = markCoverage;
                this.ligatureCoverage = ligatureCoverage;
                this.markArrayTable = markArrayTable;
                this.ligatureArrayTable = ligatureArrayTable;
            }

            public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // MarkLigPosFormat1 Subtable.
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Type               |  Name                           | Description                                          |
                // +====================+=================================+======================================================+
                // | uint16             | posFormat                       | Format identifier: format = 1                        |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | markCoverageOffset              | Offset to markCoverage table,                        |
                // |                    |                                 | from beginning of MarkLigPos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | ligatureCoverageOffset          | Offset to ligatureCoverage table,                    |
                // |                    |                                 | from beginning of MarkLigPos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | uint16             | markClassCount                  | Number of defined mark classes                       |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | markArrayOffset                 | Offset to MarkArray table, from beginning            |
                // |                    |                                 | of MarkLigPos subtable.                              |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | ligatureArrayOffset             | Offset to LigatureArray table,                       |
                // |                    |                                 | from beginning of MarkLigPos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                ushort markCoverageOffset = reader.ReadOffset16();
                ushort ligatureCoverageOffset = reader.ReadOffset16();
                ushort markClassCount = reader.ReadUInt16();
                ushort markArrayOffset = reader.ReadOffset16();
                ushort ligatureArrayOffset = reader.ReadOffset16();

                var markCoverage = CoverageTable.Load(reader, offset + markCoverageOffset);
                var ligatureCoverage = CoverageTable.Load(reader, offset + ligatureCoverageOffset);
                var markArrayTable = new MarkArrayTable(reader, offset + markArrayOffset);
                var ligatureArrayTable = new LigatureArrayTable(reader, offset + ligatureArrayOffset, markClassCount);

                return new LookupType5Format1SubTable(markCoverage, ligatureCoverage, markArrayTable, ligatureArrayTable);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }
    }
}
