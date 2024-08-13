using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadside.ViewModels
{
    
    class ResponseViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;

        public ResponseViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
        }


    }
}
