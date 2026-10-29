using System.IO;
using System.Runtime.InteropServices;

namespace Bewerbo.Services;

/// <summary>Moves files to the Windows Recycle Bin, so a file removed by mistake can be restored.</summary>
public static class RecycleBin
{
    private const uint Delete = 3;
    private const ushort AllowUndo = 0x40;
    private const ushort NoConfirmation = 0x10;
    private const ushort Silent = 0x4;
    private const ushort NoErrorUi = 0x400;

    public static void MoveFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("The file no longer exists.", path);

        var operation = new FileOperation
        {
            Function = Delete,
            From = Path.GetFullPath(path) + '\0',
            Flags = AllowUndo | NoConfirmation | Silent | NoErrorUi,
        };

        var result = SHFileOperation(ref operation);
        if (result != 0 || operation.AnyOperationsAborted)
            throw new IOException($"Windows could not move the file to the Recycle Bin (error {result}).");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FileOperation
    {
        public IntPtr Window;
        public uint Function;
        public string From;
        public string? To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool AnyOperationsAborted;
        public IntPtr NameMappings;
        public string? ProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperation(ref FileOperation operation);
}
