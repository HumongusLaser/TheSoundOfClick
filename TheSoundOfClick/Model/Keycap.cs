namespace TheSoundOfClick
{
    public class Keycap : Product
    {
        private string theme = "";
        public string Theme
        {
            get { return theme; }
            set
            {
                if (value.Length < 2)
                {
                    throw new ArgumentException("Theme of keycap must be at least 2 characters.");
                }
                theme = value;
            }
        }
        public bool IsBacklightCompatible { get; set; }


        public Keycap(Int128 id, int stock, string name, decimal price, string color, string theme, bool isBacklightCompatible) : base(id, stock, name, price, color)
        {
            Theme = theme;
            IsBacklightCompatible = isBacklightCompatible;
        }

        public override string GetProductDetails()
        {
            return $"Keycap: {Name}, Color: {Color}, Theme: {Theme}, Backlight Compatible: {IsBacklightCompatible}";
        }
    }
}
