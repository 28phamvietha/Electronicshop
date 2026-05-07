using ElectronicsShop.Models;
using ElectronicsShop.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
    public class ShipmentsController : Controller
    {
		private readonly string connectionString = ConnectionStrings.DefaultConnection;

		// GET: Shipments
		public ActionResult Index()
        {
            return View();
        }

        public List<Shipment> GetShipmentsByUserId(int userId)
        {
            List<Shipment> shipments = new List<Shipment>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        *
                    FROM
                        Shipments
                    WHERE
                        user_id = @UserId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    shipments.Add(new Shipment()
                    {
                        shipment_id = (int)reader["shipment_id"],
                        user_id = (int)reader["user_id"],
                        shipment_address = (string)reader["shipment_address"],
                        shipment_city = (string)reader["shipment_city"],
                        shipment_country = (string)reader["shipment_country"],
                        shipment_zip_code = (string)reader["shipment_zip_code"]
                    });
                }
                reader.Close();
            }
            return shipments;
        }

		public Shipment GetShipmentById(int Id)
		{
			Shipment shipment = new Shipment();

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT
                        *
                    FROM
                        Shipments
                    WHERE
                        shipment_id = @Id";

				SqlCommand cmd = new SqlCommand(query, con);
				cmd.Parameters.AddWithValue("@Id", Id);

				con.Open();
				SqlDataReader reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					shipment = new Shipment()
					{
						shipment_id = (int)reader["shipment_id"],
						user_id = (int)reader["user_id"],
						shipment_address = (string)reader["shipment_address"],
						shipment_city = (string)reader["shipment_city"],
						shipment_country = (string)reader["shipment_country"],
						shipment_zip_code = (string)reader["shipment_zip_code"]
					};
				}
				reader.Close();
			}
			return shipment;
		}
	}
}