using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizKursUno;
public class AsVbHelpers
{
    public static String GetAppVers()
    {
        return Windows.ApplicationModel.Package.Current.Id.Version.Major + "." +
        Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." +
        Windows.ApplicationModel.Package.Current.Id.Version.Build;
    }
}
