using System.Windows.Controls;

namespace GM_Desktop_Application.Views
{
    public partial class CreateCampaignView : UserControl
    {
        public CreateCampaignView()
        {
            InitializeComponent();
            Loaded += (_, _) => CampaignName.Focus();
        }
    }
}
