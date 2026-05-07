using ElectronicsShop.Models;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ElectronicsShop.Controllers
{
    public class Admin_ProductsController : Controller
    {
        private Db_ElectronicsShop db = new Db_ElectronicsShop();

        // GET: Products
        public ActionResult Index()
        {
            if ((string)Session["role"] != "admin")
            {
                return RedirectToAction("Index", "Home");
            }
            var products = db.Products
        .Include(p => p.Category)
        .Include(p => p.Product_Images);

            return View(products.ToList());
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if ((string)Session["role"] != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products
                .Include(p => p.Category)
                .Include(p => p.Product_Images)
                .Include(p => p.Specifications) // 🔥 QUAN TRỌNG
                .FirstOrDefault(p => p.product_id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            if ((string)Session["role"] != "admin")
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name");
            return View();
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if ((string)Session["role"] != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products
                .Include(p => p.Product_Images)
                .Include(p => p.Specifications)
                .FirstOrDefault(p => p.product_id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);

            return View(product);
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "product_id,category_id,product_name,description,price,discount_price,stock,brand,is_new")] Product product)
        {
            if (ModelState.IsValid)
            {
                if ((string)Session["role"] != "admin")
                {
                    return RedirectToAction("Index", "Home");
                }

                // 🔥 lưu product trước
                db.Products.Add(product);
                db.SaveChanges();

                // ================== ẢNH ==================
                var files = new[]
                {
            Request.Files["FileName1"],
            Request.Files["FileName2"],
            Request.Files["FileName3"]
        };

                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string path = Server.MapPath("~/Images/" + fileName);
                        file.SaveAs(path);

                        db.Product_Images.Add(new Product_Images
                        {
                            product_id = product.product_id,
                            image_url = fileName
                        });
                    }
                }

                db.SaveChanges();

                // ================== SPEC ==================
                var specNames = Request.Form.GetValues("spec_name");
                var specValues = Request.Form.GetValues("spec_value");
                var specGroups = Request.Form.GetValues("spec_group");

                if (specNames != null)
                {
                    for (int i = 0; i < specNames.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(specNames[i]))
                        {
                            db.Product_Specifications.Add(new Product_Specification
                            {
                                product_id = product.product_id,
                                spec_group = specGroups[i],
                                spec_name = specNames[i],
                                spec_value = specValues[i]
                            });
                        }
                    }
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "product_id,category_id,product_name,description,price,discount_price,stock,brand,is_new")] Product product)
        {
            if (ModelState.IsValid)
            {
                // 🔥 update product
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();

                // ================== ẢNH ==================
                var images = db.Product_Images
                    .Where(p => p.product_id == product.product_id)
                    .ToList();

                var files = new[]
                {
            Request.Files["FileName1"],
            Request.Files["FileName2"],
            Request.Files["FileName3"]
        };

                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];

                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string path = Server.MapPath("~/Images/" + fileName);
                        file.SaveAs(path);

                        if (i < images.Count)
                        {
                            images[i].image_url = fileName;
                        }
                        else
                        {
                            db.Product_Images.Add(new Product_Images
                            {
                                product_id = product.product_id,
                                image_url = fileName
                            });
                        }
                    }
                }

                db.SaveChanges();

                // ================== SPEC ==================
                // 🔥 xóa hết spec cũ
                var oldSpecs = db.Product_Specifications
                    .Where(s => s.product_id == product.product_id)
                    .ToList();

                db.Product_Specifications.RemoveRange(oldSpecs);
                db.SaveChanges();

                // 🔥 thêm lại spec mới
                var specNames = Request.Form.GetValues("spec_name");
                var specValues = Request.Form.GetValues("spec_value");
                var specGroups = Request.Form.GetValues("spec_group");

                if (specNames != null)
                {
                    for (int i = 0; i < specNames.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(specNames[i]))
                        {
                            db.Product_Specifications.Add(new Product_Specification
                            {
                                product_id = product.product_id,
                                spec_group = specGroups[i],
                                spec_name = specNames[i],
                                spec_value = specValues[i]
                            });
                        }
                    }
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.category_id = new SelectList(db.Categories, "category_id", "category_name", product.category_id);
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            // 🔥 XÓA WISHLIST TRƯỚC
            var wishlists = db.Wishlists
                .Where(w => w.product_id == id)
                .ToList();
            db.Wishlists.RemoveRange(wishlists);
            db.SaveChanges(); // ✅ QUAN TRỌNG

            // 🔥 XÓA SPEC
            var specs = db.Product_Specifications
                .Where(s => s.product_id == id)
                .ToList();
            db.Product_Specifications.RemoveRange(specs);
            db.SaveChanges();

            // 🔥 XÓA ẢNH
            var images = db.Product_Images
                .Where(p => p.product_id == id)
                .ToList();
            db.Product_Images.RemoveRange(images);
            db.SaveChanges();

            // 🔥 CART (THÊM MỚI)
            var carts = db.Carts.Where(c => c.product_id == id).ToList();
            db.Carts.RemoveRange(carts);

            // 🔥 XÓA PRODUCT
            db.Products.Remove(product);
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

        private void UploadImage(HttpPostedFileBase file, int productId)
        {
            string fileName = "noimage.png";

            if (file != null && file.ContentLength > 0)
            {
                fileName = System.IO.Path.GetFileName(file.FileName);

                string folder = Server.MapPath("~/Images");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string path = System.IO.Path.Combine(folder, fileName);
                file.SaveAs(path);
            }

            Product_Images pi = new Product_Images();
            pi.product_id = productId;
            pi.image_url = fileName;

            db.Product_Images.Add(pi);
        }

        DataUtil data = new DataUtil();
        public ActionResult SellingProduct()
        {
            var selling = data.GetSellingProduct();
            return View(selling);
        }
        public ActionResult UnsoldProduct()
        {
            var Unsold = data.GetUnsoldProduct();
            return View(Unsold);
        }
    }
}
