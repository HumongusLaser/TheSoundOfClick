namespace TheSoundOfClick
{
    public class Keyboard : Product
    {
        private string layout = "";
        public string Layout 
        {
            get { return layout; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Layout cannot be empty.");
                }
                layout = value;
            }
        }
        public bool IsWireless { get; set; }
        private string switchType = "";
        public string SwitchType
        {
            get { return switchType; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Switch type cannot be empty.");
                }
                switchType = value;
            }
        }

        public Keyboard(Int128 id, int stock, string name, decimal price, string color, string layout, bool isWireless, string switchType) : base(id, stock, name, price, color)
        {
            Layout = layout;
            IsWireless = isWireless;
            SwitchType = switchType;
        }

        public override string GetProductDetails()
        {
            return $"Keyboard: {Name}, Color: {Color}, Layout: {Layout}, Wireless: {IsWireless}, Switch Type: {SwitchType}";
        }
    }
}
