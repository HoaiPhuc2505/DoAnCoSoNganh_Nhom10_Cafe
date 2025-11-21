using UniCafe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UniCafe.Data;
using System.Data.Entity; // Đảm bảo bạn có dòng này


namespace UniCafe.Controllers
{
    public class CategoryController : BaseController<Category>
    {
        //private readonly IUnitOfWork _unitOfWork;
        //private readonly IRepository<Category> _categoryRepository;
        //private readonly ApplicationDbContext _context;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<OptionProduct> _optionProductRepository;
        public CategoryController()
        {
            //    _context = new ApplicationDbContext();
            //    _unitOfWork = new UnitOfWork(_context);
            //    _categoryRepository = new Repository<Category>(_context);
            _productRepository = new Repository<Product>(Context);
            _optionProductRepository = new Repository<OptionProduct>(Context);
        }
        //public void AddCategory(Category category)
        //{
        //    Repository.Add(category);
        //    UnitOfWork.BeginTransaction();
        //    try
        //    {
        //        UnitOfWork.Commit();
        //    }
        //    catch
        //    {
        //        UnitOfWork.Rollback();
        //        throw;
        //    }
        //}
        //public IEnumerable<Category> GetAllCategories()
        //{
        //    return Repository.GetAll();
        //}

        //public Category GetCategoryById(int Id)
        //{
        //    return Repository.GetById(Id);
        //}

        //public void UpdateCategory(Category category)
        //{
        //    Repository.Update(category);
        //    UnitOfWork.BeginTransaction();
        //    try
        //    {
        //        UnitOfWork.Commit();
        //    }
        //    catch
        //    {
        //        UnitOfWork.Rollback();
        //        throw;
        //    }
        //}

        //public void DeleteCategory(Category category)
        //{
        //    //Category category = _categoryRepository.GetById(Id);
        //    Repository.Remove(category);
        //    UnitOfWork.BeginTransaction();
        //    try
        //    {
        //        UnitOfWork.Commit();
        //    }
        //    catch
        //    {
        //        UnitOfWork.Rollback();
        //        throw;
        //    }
        //}
        public ActionResult Index()
        {
            var listCategory = GetAll().ToList();
            return View(listCategory);
        }
        public ActionResult Details(int Id)
        {
            Category category = GetById(Id);
            return View(category);
        }
        [HttpPost]
        public ActionResult Create(Category category)
        {
            Add(category);
            return RedirectToAction("Index", "Category");
        }
        public ActionResult Edit(int Id)
        {
            Category category = GetById(Id);
            return View(category);
        }
        [HttpPost]
        public ActionResult Edit(Category category)
        {
            Update(category);
            return RedirectToAction("Index", "Category");
        }
        public ActionResult Delete(int Id)
        {
            Category p = GetById(Id);
            return View(p);
        }
        [HttpPost]
        public ActionResult Delete(Category category)
        {
            Remove(category);
            return RedirectToAction("Index", "Category");
        }

        // #### TOÀN BỘ HÀM NÀY ĐÃ ĐƯỢC SỬA LẠI LOGIC ####
        [Route("Collections/{Slug}")]
        public ActionResult Collection(string Slug)
        {
            // DÙNG CONTEXT MỚI VÀ ĐÚNG TÊN THƯ MỤC "MODELS"
            using (var db = new UniCafe.Models.ApplicationDbContext())
            {
                // 1. Tải danh sách menu bên trái (lấy tất cả danh mục)
                var allCategories = db.Categories.Where(c => c.Status == 1).ToList();
                ViewBag.ListCate = allCategories;

                IEnumerable<Product> products;
                List<Category> categoriesToDisplay;

                if (Slug == "All" || string.IsNullOrEmpty(Slug))
                {
                    // A. NẾU LÀ TRANG "TẤT CẢ SẢN PHẨM"
                    products = db.Products.Include(p => p.Category)
                                   // SỬA LỖI 1: So sánh p.Status (string) với "1" (string)
                                   .Where(p => p.Status == "1")
                                   .ToList();

                    // Lấy tất cả danh mục cha (ParentId = 0)
                    categoriesToDisplay = db.Categories
                                            .Where(c => c.ParentId == 0 && c.Status == 1)
                                            .ToList();
                }
                else
                {
                    // B. NẾU BẤM VÀO "ĐỒ ĂN" (HOẶC "BÁNH MÌ")

                    // 1. Tìm danh mục (ví dụ: "Đồ Ăn", Id = 1)
                    var currentCategory = db.Categories.FirstOrDefault(c => c.Slug == Slug);
                    if (currentCategory == null)
                    {
                        return HttpNotFound();
                    }

                    // 2. Lấy danh sách ID của TẤT CẢ danh mục con
                    var categoryIdsToFind = db.Categories
                                              .Where(c => c.ParentId == currentCategory.Id)
                                              .Select(c => c.Id)
                                              .ToList();

                    // 3. Thêm cả ID của chính danh mục cha vào
                    categoryIdsToFind.Add(currentCategory.Id);

                    // 4. Tìm tất cả SẢN PHẨM 
                    products = db.Products
                                   .Include(p => p.Category)
                                   // SỬA LỖI 2 (DÒNG 170 CŨ): So sánh p.Status (string) với "1" (string)
                                   .Where(p => categoryIdsToFind.Contains(p.Category.Id) && p.Status == "1")
                                   .ToList();

                    // 5. SỬA LỖI LOGIC: Tìm tất cả DANH MỤC để hiển thị tiêu đề 
                    if (currentCategory.ParentId == 0)
                    {
                        // Nếu bấm vào "Đồ Ăn" (Cha), thì lấy danh sách con ("Bánh Mì", "Bánh Ngọt")
                        categoriesToDisplay = db.Categories
                                                .Where(c => c.ParentId == currentCategory.Id && c.Status == 1)
                                                .ToList();
                    }
                    else
                    {
                        // Nếu bấm vào "Bánh Mì" (Con), thì chỉ lấy chính nó
                        categoriesToDisplay = db.Categories
                                                .Where(c => c.Id == currentCategory.Id && c.Status == 1)
                                                .ToList();
                    }

                    // Xử lý trường hợp danh mục cha (Đồ Ăn) không có con, nhưng vẫn có sản phẩm
                    if (!categoriesToDisplay.Any() && products.Any())
                    {
                        categoriesToDisplay = db.Categories.Where(c => c.Id == currentCategory.Id).ToList();
                    }
                }

                ViewBag.Categories = categoriesToDisplay;
                return View(products);
            }
        }

    }
}