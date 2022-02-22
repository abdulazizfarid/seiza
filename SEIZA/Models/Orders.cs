using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SEIZA.Models
{
    public class Orders
    {
        public ArrayList order_ID = new ArrayList();
        public ArrayList Product_ID = new ArrayList();
        public ArrayList size = new ArrayList();
        public ArrayList main_title = new ArrayList();
        public ArrayList sub_title = new ArrayList();
        public ArrayList custom_date = new ArrayList();
        public ArrayList custom_time = new ArrayList();
        public ArrayList custom_location = new ArrayList();
        public ArrayList notes = new ArrayList();
        public ArrayList price = new ArrayList();
        public ArrayList paid = new ArrayList();
    }
}