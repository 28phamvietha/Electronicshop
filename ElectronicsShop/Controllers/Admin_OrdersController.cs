using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ElectronicsShop.Models;


namespace ElectronicsShop.Controllers
{
    public class Admin_OrdersController : Controller
    {
        private Db_ElectronicsShop db = new Db_ElectronicsShop();

        // GET: Orders
        public ActionResult Index()
        {
            var orders = db.Orders.Include(o => o.Shipment).Include(o => o.User);
            return View(orders.ToList());
        }

        // GET: Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include(o => o.Shipment)
                .Include(o => o.User)
                .Include(o => o.Order_Items.Select(oi => oi.Product)) // load luôn product
                .FirstOrDefault(o => o.order_id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public ActionResult Create()
        {
            ViewBag.shipment_id = new SelectList(db.Shipments, "shipment_id", "shipment_address");
            ViewBag.user_id = new SelectList(db.Users, "user_id", "first_name");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "order_id,shipment_id,user_id,order_date,total_amount,status,payment_method,order_note")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.shipment_id = new SelectList(db.Shipments, "shipment_id", "shipment_address", order.shipment_id);
            ViewBag.user_id = new SelectList(db.Users, "user_id", "first_name", order.user_id);
            return View(order);
        }

        // GET: Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.shipment_id = new SelectList(db.Shipments, "shipment_id", "shipment_address", order.shipment_id);
            ViewBag.user_id = new SelectList(db.Users, "user_id", "first_name", order.user_id);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "order_id,shipment_id,user_id,order_date,total_amount,status,payment_method,order_note")] Order order)
        {
            if (ModelState.IsValid)
            {
                //[Bind(Include = "order_id,shipment_id,user_id,order_date,total_amount,status,payment_method,order_note")]
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.shipment_id = new SelectList(db.Shipments, "shipment_id", "shipment_address", order.shipment_id);
            ViewBag.user_id = new SelectList(db.Users, "user_id", "first_name", order.user_id);
            return View(order);
        }

        // GET: Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // New: action returns aggregated order data for charts/tables.
        // Expected query params from UI: ?agg=day|month|year&start=yyyy-MM-dd&end=yyyy-MM-dd
        public JsonResult GetOrdersData(string agg, string start, string end)
        {
            // Parse dates, fallback to last 30 days
            DateTime startDate, endDate;
            var today = DateTime.Today;
            if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            {
                startDate = today.AddDays(-29);
            }
            if (!DateTime.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
            {
                endDate = today;
            }

            // Normalize range (inclusive end)
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            // Ensure agg value
            agg = (agg ?? "day").ToLowerInvariant();

            // Query orders within range
            var ordersInRange = db.Orders
                .Where(o => o.order_date != null && o.order_date >= startDate && o.order_date <= endDate);

            // Prepare result containers
            var labels = new List<string>();
            var counts = new List<int>();
            var revenues = new List<decimal>();

            if (agg == "day")
            {
                var q = ordersInRange
                    .GroupBy(o => new { o.order_date.Value.Year, o.order_date.Value.Month, o.order_date.Value.Day })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Count = g.Count(),
                        Revenue = g.Sum(x => (decimal?)x.total_amount) ?? 0M
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                    .ToList();

                foreach (var r in q)
                {
                    labels.Add(new DateTime(r.Year, r.Month, r.Day).ToString("dd/MM/yyyy"));
                    counts.Add(r.Count);
                    revenues.Add(r.Revenue);
                }
            }
            else if (agg == "month")
            {
                var q = ordersInRange
                    .GroupBy(o => new { o.order_date.Value.Year, o.order_date.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                        Revenue = g.Sum(x => (decimal?)x.total_amount) ?? 0M
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                foreach (var r in q)
                {
                    labels.Add($"{r.Month:00}/{r.Year}");
                    counts.Add(r.Count);
                    revenues.Add(r.Revenue);
                }
            }
            else if (agg == "year")
            {
                var q = ordersInRange
                    .GroupBy(o => o.order_date.Value.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(x => (decimal?)x.total_amount) ?? 0M
                    })
                    .OrderBy(x => x.Year)
                    .ToList();

                foreach (var r in q)
                {
                    labels.Add(r.Year.ToString());
                    counts.Add(r.Count);
                    revenues.Add(r.Revenue);
                }
            }
            else
            {
                // fallback: daily
                return GetOrdersData("day", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            }

            // Return JSON in expected shape: { labels: [...], orders: [...], revenue: [...] }
            return Json(new
            {
                labels = labels,
                orders = counts,
                revenue = revenues
            }, JsonRequestBehavior.AllowGet);
        }

        // Keep the view action
        public ActionResult Statistical()
        {
            return View();
        }
    }
}
