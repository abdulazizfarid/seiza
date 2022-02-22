using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Net.Mail;

namespace SEIZA.Controllers
{
    public class HomeController : Controller
    {
        public static Random random = new Random(DateTime.Now.Ticks.GetHashCode());
        public static SEIZA.Models.Cart cart = new SEIZA.Models.Cart();
        public static SEIZA.Models.Details details = new SEIZA.Models.Details();
        public static SEIZA.Models.Orders orders = new SEIZA.Models.Orders();
        public static ArrayList cartIDs = new ArrayList();
        private string connStr;
        public static int cartCount = 0;
        public static int[,] tempPrices = new int[25, 2];
        public static int order_ID = HomeController.randomNumGen();
        public static int randomNumGen()
        {
            int rndNum = HomeController.random.Next(0, 2000000000);
            return rndNum;
        }

        public HomeController()
        {
            connStr = "Data Source=LAIFU-PC\\SQLEXPRESS;Initial Catalog=SEIZA;Integrated Security=True;";

        }
            
        public ActionResult Index()
        {
            ViewBag.cartCount = cartIDs.Count;
            ViewBag.Title = "S E I Z A | Personalized Star Maps";
            return View();
        }

        public ActionResult Info()
        {
            ViewBag.cartCount = cartIDs.Count;
            ViewBag.Title = "Info - S E I Z A";
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Designs()
        {
            ViewBag.cartCount = cartIDs.Count;
            var rec = new SEIZA.Models.Data();
            string[] prod_name = new string[25];
            string[] img = new string[25];
            int[] prod_ID = new int[25];
            var data = GetDesigns(); 
            ViewBag.count = data.Rows.Count;
                for(int i=0; i < data.Rows.Count; i++)
            {
                rec.Product_Name = data.Rows[i]["Product_Name"].ToString();
                rec.img_src = data.Rows[i]["img_src"].ToString();
                rec.prod_ID = Convert.ToInt32(data.Rows[i]["Product_ID"]);
                prod_name[i] = rec.Product_Name;
                img[i] = rec.img_src;
                prod_ID[i] = rec.prod_ID;

            }
            ViewBag.Title = "Designs - S E I Z A";
            ViewBag.names = prod_name;
            ViewBag.src = img;
            ViewBag.prodID = prod_ID;
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.cartCount = cartIDs.Count;
            ViewBag.Title = "Contact - S E I Z A";
            ViewBag.Message = "Your Products";

            return View();
        }

        public ActionResult Cart()
        {   
            string[] prod_name = new string[25];
            string[] img = new string[25];
            int[] prod_ID = new int[25];
            int[] prices = new int[25];
            int[] tempIDs = cartIDs.ToArray(typeof(int)) as int[];
            var data = GetAddtoCartDetails(tempIDs);
            if (tempIDs.Count()>0) {               
                for (int i = 0; i < cartIDs.Count; i++)
                {
                    prod_name[i] = cart.Product_Name[i].ToString();
                    img[i] = cart.img_src[i].ToString();
                    prod_ID[i] = Convert.ToInt32(cart.prod_ID[i]);
                    prices[i] = Convert.ToInt32(cart.price[i]);

                }
                cartCount = data.Rows.Count;
            }
            else
            {
                cartCount = 0;
            }
            
            ViewBag.Title = "Cart - S E I Z A";
            //ViewBag.cartCount = cartCount;
            ViewBag.cartCount = cartIDs.Count;
            ViewBag.cartNames = prod_name;
            ViewBag.cartSrc = img;
            ViewBag.cartProdID = prod_ID;
            ViewBag.cartPrices = prices;
            return View();
        }

        [HttpPost]
        public ActionResult Cart(FormCollection form)
        {
            string[] prod_name = new string[25];
            string[] img = new string[25];
            int[] prod_ID = new int[25];
            string size = form.Get("size");
            
            int price = 0;
            if (size == "A3") price = 3200;
            else if (size == "A4") price = 2800;
            int[] prices = new int[25];

            string mode = form.Get("mode");
            if (mode == "Add")
            {
                AddOrder(form);
                int Priceindex = cartIDs.Count;
                cartIDs.Add(Convert.ToInt32(form.Get("ProductID")));
                tempPrices[Priceindex, 0] = Convert.ToInt32(form.Get("ProductID"));
                tempPrices[Priceindex,1] = price;
                cart.price.Add(price);
            }
            else if (mode == "Remove")
            {
                int removeID = Convert.ToInt32(form.Get("RemoveID"));
                RemoveOrder(removeID);
                int found = 0;
                int tempCount = cartIDs.Count-1;
                for(int k = 0; k < cartIDs.Count; k++)
                {
                    if (found == 0 && k<tempCount)
                    {
                        if (tempPrices[k, 0] == removeID)
                        {
                            found = 1;
                            tempPrices[k, 0] = tempPrices[k + 1, 0];
                            tempPrices[k, 1] = tempPrices[k + 1, 1];
                        }
                    }
                    else if (found == 1 && k<tempCount)
                    {
                        tempPrices[k, 0] = tempPrices[k + 1, 0];
                        tempPrices[k, 1] = tempPrices[k + 1, 1];
                    }
                    if (found == 0 && k == tempCount)
                    {
                        if (tempPrices[k, 0] == removeID)
                        {
                            found = 1;
                            tempPrices[k, 0] = 0;
                            tempPrices[k, 1] = 0;
                        }
                    }

                }
                cartIDs.Remove(removeID);
                int removeIndex = cart.prod_ID.IndexOf(removeID);
                cart.price.RemoveAt(removeIndex);
                
            }
            int[] tempIDs = cartIDs.ToArray(typeof(int)) as int[];
            cart.Product_Name.Clear();
            cart.img_src.Clear();
            cart.prod_ID.Clear();
            //cart.price.Clear();
            
            var data = GetAddtoCartDetails(tempIDs);
            if (tempIDs.Count() > 0)
            {
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    cart.Product_Name.Add(data.Rows[i]["Product_Name"].ToString());
                    cart.img_src.Add(data.Rows[i]["img_src"].ToString());
                    cart.prod_ID.Add(Convert.ToInt32(data.Rows[i]["Product_ID"]));
                    var sizeData = GetSize(size);
                    //cart.price.Add(Convert.ToInt32(sizeData.Rows[i]["Price"]));
                    prod_name[i] = cart.Product_Name[i].ToString();
                    img[i] = cart.img_src[i].ToString();
                    prod_ID[i] = Convert.ToInt32(cart.prod_ID[i]);
                    //prices[i] = 2800;
                    //prices[i] = Convert.ToInt32(cart.price[i]);
                }
                cartCount = data.Rows.Count;
                for (int i=0; i < cart.prod_ID.Count; i++)
                {
                    //Check all product IDs in tempprices and get their prices;
                    for (int j = 0; j < cart.prod_ID.Count; j++)
                    {
                        if (tempPrices[j, 0] == Convert.ToInt32(cart.prod_ID[i]))
                        {
                            prices[i] = tempPrices[j, 1];
                        }
                    }

                }
            }
            else
            {
                cartCount = 0;
            }
            ViewBag.Title = "Cart - S E I Z A";
            ViewBag.cartCount = cartIDs.Count;
            ViewBag.cartNames = prod_name;
            ViewBag.cartSrc = img;
            ViewBag.cartProdID = prod_ID;
            ViewBag.cartPrices = prices;
            return View();
        }
            
        [HttpPost]
        public ActionResult ItemPage(FormCollection form)
        {
            ViewBag.cartCount = cartIDs.Count;
            string ProdName = form.Get("ProductName");
            var rec = new SEIZA.Models.Data();
            string[] prod_name = new string[25];
            string[] img = new string[25];
            int[] prod_ID = new int[25];
            ViewData["ProductName"] = ProdName;
            var data = GetItem(ViewData["ProductName"].ToString());
            ViewBag.count = data.Rows.Count;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                rec.Product_Name = data.Rows[i]["Product_Name"].ToString();
                rec.img_src = data.Rows[i]["img_src"].ToString();
                rec.prod_ID = Convert.ToInt32(data.Rows[i]["Product_ID"]);
                prod_name[i] = rec.Product_Name;
                img[i] = rec.img_src;
                prod_ID[i] = rec.prod_ID;

            }
            ViewBag.Title = "Designs - S E I Z A";
            ViewBag.names = prod_name;
            ViewBag.src = img;
            ViewBag.prodID = prod_ID;
            return View();
        }

        [HttpPost]
        public ActionResult Checkout(FormCollection detailsForm)
        {
            AddDetails(detailsForm);
            CommitDetails();
            CommitOrder();
            sendMail(details.Email[0].ToString());
            cartIDs.Clear();
            clearDetails();
            clearOrders();
            clearCart();
            ViewBag.orderID = order_ID;

            order_ID = randomNumGen();

            return View();
        }
        public ActionResult Details()
        {
            ViewBag.cartCount = cartIDs.Count;
            return View();
        }
        private DataTable GetAddtoCartDetails(int[] IDs)
        {
            SqlConnection connection = new SqlConnection(connStr);
            SqlCommand cmd = connection.CreateCommand();
            try
            {
                string queryIDs = "";
                for (int i = 0; i < IDs.Count(); i++)
                {
                    if (i == 0) queryIDs = IDs[0].ToString();
                    else queryIDs = queryIDs + ", " + IDs[i];
                }
                string getUsersQuery = "Select Products.Product_ID, Product_Name, img_src FROM Products inner join Images ON Products.Product_ID = Images.Product_ID WHERE Products.Product_ID IN (" + queryIDs + ");";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");
                cmd.CommandText = getUsersQuery;

                connection.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                DataTable dt = new DataTable(reader.ToString());
                //DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error";
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        private DataTable GetDesigns()
        {
            SqlConnection connection = new SqlConnection(connStr);
            SqlCommand cmd = connection.CreateCommand();
            try
            {
                string getUsersQuery = "Select Products.Product_ID, Product_Name, img_src FROM Products inner join Images ON Products.Product_ID = Images.Product_ID;";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");
                cmd.CommandText = getUsersQuery;

                connection.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                DataTable dt = new DataTable(reader.ToString());
                //DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error";
                return null;
            }
            finally
            {
                connection.Close();
            }
        }
        
        private DataTable GetItem(string ProductName)
        {
            SqlConnection connection = new SqlConnection(connStr);
            SqlCommand cmd = connection.CreateCommand();
            try
            {
                string getUsersQuery = "Select Products.Product_ID, Product_Name, img_src FROM Products inner join Images ON Products.Product_ID = Images.Product_ID WHERE Products.Product_ID = (SELECT Product_ID from Products WHERE Product_Name='" + ViewData["ProductName"]+ "');";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");
                cmd.CommandText = getUsersQuery;

                connection.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                DataTable dt = new DataTable(reader.ToString());
                //DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception ex)
            {
                
                TempData["Error"] = "Error";
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        private DataTable GetSize(string Size)
        { 
            SqlConnection connection = new SqlConnection(connStr);
            SqlCommand cmd = connection.CreateCommand();
            try
            {
                string getUsersQuery = "Select * from Price Where Size = '" + Size + "';";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");
                cmd.CommandText = getUsersQuery;

                connection.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                DataTable dt = new DataTable(reader.ToString());
                //DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error";
                return null;
            }
            finally
            {
                connection.Close();
            }

        }

        public void AddOrder(FormCollection orderForm)
        {
            int productID = Convert.ToInt32(orderForm.Get("productID"));
            string size = orderForm.Get("size");
            string mainTitle = orderForm.Get("maintitle");
            string subTitle = orderForm.Get("subtitle");
            string customDate = orderForm.Get("date");
            string time = orderForm.Get("time");
            string location = orderForm.Get("location");
            string notes = orderForm.Get("notes");
            int price = 0;

            if (size == "A3") price = 3200;
            else if (size == "A4") price = 2800;

            orders.order_ID.Add(order_ID);
            orders.Product_ID.Add(productID);
            orders.size.Add(size);
            orders.main_title.Add(mainTitle);
            orders.sub_title.Add(subTitle);
            orders.custom_date.Add(customDate);
            orders.custom_time.Add(time);
            orders.custom_location.Add(location);
            orders.notes.Add(notes);
            orders.price.Add(price);
            orders.paid.Add("No");
        }

        public void RemoveOrder(int removeID)
        {
            int removeIndex = orders.Product_ID.IndexOf(removeID);
            orders.order_ID.RemoveAt(removeIndex);
            orders.Product_ID.RemoveAt(removeIndex);
            orders.size.RemoveAt(removeIndex);
            orders.main_title.RemoveAt(removeIndex);
            orders.sub_title.RemoveAt(removeIndex);
            orders.custom_date.RemoveAt(removeIndex);
            orders.custom_time.RemoveAt(removeIndex);
            orders.custom_location.RemoveAt(removeIndex);
            orders.notes.RemoveAt(removeIndex);
            orders.price.RemoveAt(removeIndex);
            orders.paid.RemoveAt(removeIndex);
        }

        public void AddDetails(FormCollection detailsForm)
        {
            string fname = detailsForm.Get("fname");
            string lname = detailsForm.Get("lname");
            string num = detailsForm.Get("num");
            string email = detailsForm.Get("email");
            string address = detailsForm.Get("address");

            details.Order_ID.Add(order_ID);
            details.First_Name.Add(fname);
            details.Last_Name.Add(lname);
            details.Number.Add(num);
            details.Email.Add(email);
            details.Address.Add(address);
        }
        public void CommitDetails()
        {
            SqlConnection connection = new SqlConnection(connStr);
            //SqlCommand cmd = connection.CreateCommand();
            try
            {
                //string getUsersQuery = "INSERT INTO Details VALUES (" + order_ID + ",'" + details.First_Name + "','" + details.Last_Name + "','" + details.Number + "','" + details.Email + "','" + details.Address + "');";
                string getUserQuery = "INSERT INTO Details VALUES(@orderID, @first_name, @last_name, @number, @email, @address);";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");'
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(getUserQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@orderID", order_ID);
                    cmd.Parameters.AddWithValue("@first_name", details.First_Name[0].ToString());
                    cmd.Parameters.AddWithValue("@last_name", details.Last_Name[0].ToString());
                    cmd.Parameters.AddWithValue("@number", details.Number[0].ToString());
                    cmd.Parameters.AddWithValue("@email", details.Email[0].ToString());
                    cmd.Parameters.AddWithValue("@address", details.Address[0].ToString());

                    int result = cmd.ExecuteNonQuery();
                    if (result < 0)
                        Console.WriteLine("Error inserting data into Database!");
                }
                //cmd.CommandText = getUsersQuery;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error";
                //return null;
            }
            finally
            {
                connection.Close();
            }
        }
        public void CommitOrder()
        {
            SqlConnection connection = new SqlConnection(connStr);
            //SqlCommand cmd = connection.CreateCommand();
            try
            {
                //string getUsersQuery = "INSERT INTO Details VALUES (" + order_ID + ",'" + details.First_Name + "','" + details.Last_Name + "','" + details.Number + "','" + details.Email + "','" + details.Address + "');";
                string getUserQuery = "INSERT INTO Orders(order_ID,Product_ID,size,main_title,sub_title,custom_date,custom_time,custom_location,notes,price,Paid) VALUES(@order_ID, @Product_ID, @size, @main_title, @sub_title, @custom_date, @custom_time, @custom_location, @notes, @price, @Paid);";
                //getUsersQuery = getUsersQuery.Replace("@Email", Email);
                //cmd.Parameters.AddWithValue("@Email", "admin");'
                connection.Open();
                for (int i=0; i<orders.order_ID.Count; i++)
                {
                    using (SqlCommand cmd = new SqlCommand(getUserQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@order_ID", order_ID);
                        cmd.Parameters.AddWithValue("@Product_ID", orders.Product_ID[i].ToString());
                        cmd.Parameters.AddWithValue("@size", orders.size[i].ToString());
                        cmd.Parameters.AddWithValue("@main_title", orders.main_title[i].ToString());
                        cmd.Parameters.AddWithValue("@sub_title", orders.sub_title[i].ToString());
                        cmd.Parameters.AddWithValue("@custom_date", orders.custom_date[i].ToString());
                        cmd.Parameters.AddWithValue("@custom_time", orders.custom_time[i].ToString());
                        cmd.Parameters.AddWithValue("@custom_location", orders.custom_location[i].ToString());
                        cmd.Parameters.AddWithValue("@notes", orders.notes[i].ToString());
                        cmd.Parameters.AddWithValue("@price", orders.price[i].ToString());
                        cmd.Parameters.AddWithValue("@Paid", orders.paid[i].ToString());

                        int result = cmd.ExecuteNonQuery();
                        if (result < 0)
                            Console.WriteLine("Error inserting data into Database!");
                    }
                }
                
                //cmd.CommandText = getUsersQuery;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error";
                //return null;
            }
            finally
            {
                connection.Close();
            }
        }
        public void clearDetails()
        {
            details.Order_ID.Clear();
            details.First_Name.Clear();
            details.Last_Name.Clear();
            details.Number.Clear();
            details.Email.Clear();
            details.Address.Clear();
        }

        public void clearOrders()
        {
            orders.order_ID.Clear();
            orders.Product_ID.Clear();
            orders.size.Clear();
            orders.main_title.Clear();
            orders.sub_title.Clear();
            orders.custom_date.Clear();
            orders.custom_time.Clear();
            orders.custom_location.Clear();
            orders.notes.Clear();
            orders.price.Clear();
            orders.paid.Clear();
        }

        public void clearCart()
        {
            cart.img_src.Clear();
            cart.prod_ID.Clear();
            cart.Product_Name.Clear();
            cart.price.Clear();
        }

        public void sendMail(string email)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com", 587);
                SmtpServer.EnableSsl = true;
                SmtpServer.Timeout = 10000;
                SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Credentials = new System.Net.NetworkCredential("seiza.space@gmail.com", "S3iZa@@@");

                mail.From = new MailAddress("seiza.space@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Your order has been confirmed!";
                mail.Body = "Dear " + details.First_Name[0] + ", \n\nYour order number #" + details.Order_ID[0] + " has been confirmed and will be delivered within 2-3 working days!\n\n Your top provider of tangible astronomical moments,\n S E I Z A";

                //SmtpServer.Port = 587;
                
                //SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                Console.WriteLine("Mail Sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }


}