using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XePatcher
{
    public class COFF
    {
        #region Enums

        public enum FILHDR_FLAGS : ushort
        {
            // Header flags
            F_RELFLG =  0x0001,
            F_EXEC =    0x0002,
            F_LNNO =    0x0004,
            F_LSYMS =   0x0008,
            F_AR32WR =  0x0100,
        }

        public enum SCNHDR_FLAGS : uint
        {
            // Section flags
            STYP_TEXT = 0x0020,
            STYP_DATA = 0x0040,
            STYP_BSS =  0x0080,
        }

        #endregion

        #region Structures

        public struct FILHDR
        {
            public ushort   f_magic;         /* magic number             */
            public ushort   f_nscns;         /* number of sections       */
            public uint     f_timdat;        /* time & date stamp        */
            public uint     f_symptr;        /* file pointer to symtab   */
            public uint     f_nsyms;         /* number of symtab entries */
            public ushort   f_opthdr;        /* sizeof(optional hdr)     */
            public ushort   f_flags;         /* flags                    */

            public const int kSizeOf = 20;
        }

        public struct SCNHDR
        {
            public char[]   s_name;     /* section name                     */
            public uint     s_paddr;    /* physical address, aliased s_nlib */
            public uint     s_vaddr;    /* virtual address                  */
            public uint     s_size;     /* section size                     */
            public uint     s_scnptr;   /* file ptr to raw data for section */
            public uint     s_relptr;   /* file ptr to relocation           */
            public uint     s_lnnoptr;  /* file ptr to line numbers         */
            public ushort   s_nreloc;   /* number of relocation entries     */
            public ushort   s_nlnno;    /* number of line number entries    */
            public uint     s_flags;    /* flags                            */

            public byte[] data;
        }

        #endregion

        public readonly char[] TEXT_SECTION_NAME = { '.', 't', 'e', 'x', 't', '\0', '\0', '\0' };
        public readonly char[] DATA_SECTION_NAME = { '.', 'd', 'a', 't', 'a', '\0', '\0', '\0' };

        public const short I386MAGIC = 0x014C;

        private string fileName;

        private FILHDR m_header;
        private SCNHDR[] m_sections;

        public COFF(string filePath)
        {
            // Save the file path.
            this.fileName = filePath;

            // Open the coff file for reading.
            System.IO.BinaryReader reader = new System.IO.BinaryReader(new System.IO.FileStream(
                fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read));

            // Read the header.
            m_header = new FILHDR();
            m_header.f_magic = reader.ReadUInt16();
            m_header.f_nscns = reader.ReadUInt16();
            m_header.f_timdat = reader.ReadUInt32();
            m_header.f_symptr = reader.ReadUInt32();
            m_header.f_nsyms = reader.ReadUInt32();
            m_header.f_opthdr = reader.ReadUInt16();
            m_header.f_flags = reader.ReadUInt16();

            // Check the header magic.
            if (m_header.f_magic != I386MAGIC)
                throw new Exception("COFF file is of unsupported type!");

            // Seek to the section headers start.
            reader.BaseStream.Position = FILHDR.kSizeOf + m_header.f_opthdr;

            // Read the section headers from the file.
            m_sections = new SCNHDR[m_header.f_nscns];
            for (int i = 0; i < m_header.f_nscns; i++)
            {
                // Read the section header from the file.
                m_sections[i] = new SCNHDR();
                m_sections[i].s_name = reader.ReadChars(8);
                m_sections[i].s_paddr = reader.ReadUInt32();
                m_sections[i].s_vaddr = reader.ReadUInt32();
                m_sections[i].s_size = reader.ReadUInt32();
                m_sections[i].s_scnptr = reader.ReadUInt32();
                m_sections[i].s_relptr = reader.ReadUInt32();
                m_sections[i].s_lnnoptr = reader.ReadUInt32();
                m_sections[i].s_nreloc = reader.ReadUInt16();
                m_sections[i].s_nlnno = reader.ReadUInt16();
                m_sections[i].s_flags = reader.ReadUInt32();
            }

            // Read out the data for each section header.
            for (int i = 0; i < m_sections.Length; i++)
            {
                // Check to see if this section has any data for it.
                if (m_sections[i].s_scnptr > 0 && m_sections[i].s_size > 0)
                {
                    // Seek to the section's data offset.
                    reader.BaseStream.Position = m_sections[i].s_scnptr;

                    // Read the data into a buffer.
                    m_sections[i].data = reader.ReadBytes((int)m_sections[i].s_size);
                }
            }

            // Close the reader.
            reader.Close();
        }

        public byte[] GetTextSectionData()
        {
            // Loop through the section headers.
            for (int i = 0; i < m_sections.Length; i++)
            {
                // Check the section name and flags for that of the .text section.
                if (CharArrayCompare(m_sections[i].s_name, TEXT_SECTION_NAME) == true &&
                    (m_sections[i].s_flags & (uint)SCNHDR_FLAGS.STYP_TEXT) == (uint)SCNHDR_FLAGS.STYP_TEXT)
                {
                    // We found it.
                    return m_sections[i].data;
                }
            }

            // Not found.
            return null;
        }

        private bool CharArrayCompare(char[] a, char[] b)
        {
            // Check that they have the same length.
            if (a.Length != b.Length)
                return false;

            // Loop and compare the arrays.
            for (int i = 0; i < a.Length; i++)
            {
                // Check the characters.
                if (a[i].Equals(b[i]) == false)
                    return false;
            }

            // They match, return true.
            return true;
        }
    }
}
