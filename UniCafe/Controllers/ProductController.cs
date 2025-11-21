using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UniCafe.Data;
using UniCafe.Models;
using System.Runtime.Caching;
using System.Data.Entity;

namespace UniCafe.Controllers
{
    public class ProductController : BaseController<Product>
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<PropertyProduct> _propertyProductRepositoy;
        private readonly IRepository<OptionProduct> _optionProductRepositoy;
        public ProductController()
        {
            _categoryRepository = new Repository<Category>(Context);
            _propertyProductRepositoy = new Repository<PropertyProduct>(Context);
            _optionProductRepositoy = new Repository<OptionProduct>(Context);
        }
        /// <summary>
        ///  MEMORY CACHE
        /// </summary>
        /// <returns></returns>
        private static MemoryCache _productCache = new MemoryCache("ProductCache");
        public List<Product> GetProducts()
        {
            var cacheKey = "ProductCache";
            var productList = _productCache.Get(cacheKey) as List<Product>;

            if (productList == null)
            {
                productList = GetAll().ToList();
                //productList = Context.Products.Where(s => s.Name == "123").ToList();
                var cachePolicy = new CacheItemPolicy();
                cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1);
                _productCache.Set(cacheKey, productList, cachePolicy);
            }
            return productList;
        }
        public ActionResult Index()
        {
            //var p = GetProducts();
            var p = GetAll().ToList();
            var c = _categoryRepository.GetAll().ToList();
            ViewBag.Categories = c;
            ViewBag.PropertyProducts = _propertyProductRepositoy.GetAll().ToList();
            ViewBag.OptionProducts = _optionProductRepositoy.GetAll().ToList();
            return View(p);
        }

        [Route("Product/{Slug}")]
        public ActionResult Details(string Slug)
        {
            try
            {
                var product = Context.Products
                                     .Include(p => p.Category)
                                     .FirstOrDefault(x => x.Slug == Slug);

                if (product == null || Slug == null)
                {
                    return Redirect("/Collections/All");
                }

                ViewBag.propertyProducts = Context.PropertityProducts
                                                .Where(x => x.Product.Slug == Slug)
                                                .OrderBy(x => x.Price)
                                                .ToList();

                ViewBag.OptionProducts = Context.OptionProducts
                                              .Where(x => x.Product.Slug == Slug)
                                              .ToList();

                ViewBag.RelatedProducts = Context.Products
                                               .Where(p => p.Category.Id == product.Category.Id && p.Id != product.Id)
                                               .Take(4) 
                                               .ToList();

                return View(product);
            }
            catch (Exception)
            {
                return Redirect("/Collections/All");
            }
        }
    }
}