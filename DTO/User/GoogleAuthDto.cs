using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FocusMapApi.DTO.User
{
    public class GoogleAuthDto
    {
        public string Token { get; set; }
    }
    public class GoogleLoginDto
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public string Sub { get; set; }
    }
}
