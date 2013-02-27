using System;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System.Runtime.InteropServices;

//using WindowsMediaLib;
using System.Security.Principal;
using AsfMojo.Configuration;

namespace AsfMojo.Media
{
    /// <summary>
    /// In-Memory ASF stream that may be passed as stream source to an IWMSyncReader 
    /// </summary>
    public class AsfIStream : IStream
    {
        private Stream _baseStream;

        public AsfIStream(Stream stream)
        {
            _baseStream = stream;
        }


        ~AsfIStream()
        {
           Close();
        }

        public void Close()
        {
            _baseStream.Dispose();
        }

        // Summary:
        //     Creates a new stream object with its own seek pointer that references the
        //     same bytes as the original stream.
        //
        // Parameters:
        //   ppstm:
        //     When this method returns, contains the new stream object. This parameter
        //     is passed uninitialized.
        public void Clone(out IStream ppstm)
        {
            ppstm = null;
            //Not Implemented
        }
        //
        public void Commit(int grfCommitFlags)
        {
            //Not Implemented
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            //Not Implemented
        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            //Not Implemented

        }
        //
        // Summary:
        //     Reads a specified number of bytes from the stream object into memory starting
        //     at the current seek pointer.
        //
        // Parameters:
        //   pv:
        //     When this method returns, contains the data read from the stream. This parameter
        //     is passed uninitialized.
        //
        //   cb:
        //     The number of bytes to read from the stream object.
        //
        //   pcbRead:
        //     A pointer to a ULONG variable that receives the actual number of bytes read
        //     from the stream object.
        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            int bytesRead = _baseStream.Read(pv, 0, cb);

            if (pcbRead != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.WriteInt32(pcbRead, bytesRead);
            }
        }
        //
        // Summary:
        //     Discards all changes that have been made to a transacted stream since the
        //     last System.Runtime.InteropServices.ComTypes.IStream.Commit(System.Int32)
        //     call.
        public void Revert()
        { 
            //Not Implemented
        }
        //
        // Summary:
        //     Changes the seek pointer to a new location relative to the beginning of the
        //     stream, to the end of the stream, or to the current seek pointer.
        //
        // Parameters:
        //   dlibMove:
        //     The displacement to add to dwOrigin.
        //
        //   dwOrigin:
        //     The origin of the seek. The origin can be the beginning of the file, the
        //     current seek pointer, or the end of the file.
        //
        //   plibNewPosition:
        //     On successful return, contains the offset of the seek pointer from the beginning
        //     of the stream.
        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            long currentOffset = _baseStream.Position;

            SeekOrigin seekOrigin = (SeekOrigin)dwOrigin;

            switch (seekOrigin)
            {
                case SeekOrigin.Begin:
                    currentOffset = dlibMove;
                    break;
                case SeekOrigin.Current:
                    currentOffset += dlibMove;
                    break;
                case SeekOrigin.End:
                    currentOffset = _baseStream.Length - 1;
                    break;
            }

            if (currentOffset >= _baseStream.Length)
                currentOffset = _baseStream.Length - 1;

            if (currentOffset <= AsfConstants.ASF_MAX_HEADER_SIZE)
            {
                _baseStream.Seek(currentOffset, SeekOrigin.Begin);

                if (plibNewPosition != IntPtr.Zero)
                {
                    Marshal.WriteInt64(plibNewPosition, currentOffset);
                }
            }
        }
        //
        // Summary:
        //     Changes the size of the stream object.
        //
        // Parameters:
        //   libNewSize:
        //     The new size of the stream as a number of bytes.
        public void SetSize(long libNewSize)
        {

        }
        //
        // Summary:
        //     Retrieves the System.Runtime.InteropServices.STATSTG structure for this stream.
        //
        // Parameters:
        //   pstatstg:
        //     When this method returns, contains a STATSTG structure that describes this
        //     stream object. This parameter is passed uninitialized.
        //
        //   grfStatFlag:
        //     Members in the STATSTG structure that this method does not return, thus saving
        //     some memory allocation operations.
        public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG();
            pstatstg.cbSize = _baseStream.Length;
            pstatstg.type = 2;
        }
        //
        // Summary:
        //     Removes the access restriction on a range of bytes previously restricted
        //     with the System.Runtime.InteropServices.ComTypes.IStream.LockRegion(System.Int64,System.Int64,System.Int32)
        //     method.
        //
        // Parameters:
        //   libOffset:
        //     The byte offset for the beginning of the range.
        //
        //   cb:
        //     The length, in bytes, of the range to restrict.
        //
        //   dwLockType:
        //     The access restrictions previously placed on the range.
        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
        }
    }
}
