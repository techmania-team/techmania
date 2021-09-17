namespace FantomLib
{
    /// <summary>
    /// Android Colors (AARRGGBB format: int32)
    /// 
    /// Android の色定数（ARGB 形式の int 値）
    /// https://developer.android.com/reference/android/graphics/Color.html#BLACK
    /// </summary>
    public static class AndroidColor
    {
        public const int BLACK = 0x000000 | (0xff << 24);
        public const int BLUE = 0x0000ff | (0xff << 24);
        public const int CYAN = 0x00ffff | (0xff << 24);
        public const int DKGRAY = 0x444444 | (0xff << 24);
        public const int GRAY = 0x888888 | (0xff << 24);
        public const int GREEN = 0x00ff00 | (0xff << 24);
        public const int LTGRAY = 0xcccccc | (0xff << 24);
        public const int MAGENTA = 0xff00ff | (0xff << 24);
        public const int RED = 0xff0000 | (0xff << 24);
        public const int TRANSPARENT = 0x000000 | (0xff << 24);
        public const int WHITE = 0xffffff | (0xff << 24);
        public const int YELLOW = 0xffff00 | (0xff << 24);
    }
}