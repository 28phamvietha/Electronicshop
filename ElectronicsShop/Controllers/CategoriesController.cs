using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using ElectronicsShop.Models;
using System.Configuration;

namespace ElectronicsShop.Controllers
{
    public class CategoriesController : Customer_BaseController
    {
        // Database connection string
        //private string connectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=thuongmaidientudb;Integrated Security=True;TrustServerCertificate=True";
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        // GET: Categories
        public ActionResult Index()
        {
            return View();
        }

        public List<Category> GetCategories()
        {
            List<Category> categories = new List<Category>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT category_id, category_name FROM Categories";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandTimeout = 30;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                category_id = (int)reader["category_id"],
                                category_name = reader["category_name"].ToString()
                            });
                        }
                    }
                }
            }

            return categories;
        }
    }
}