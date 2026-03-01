using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controller;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private ProductService  _service = new ProductService();

    // public ProductController(IProductService service)
    // {
    //     _service = service ;
    // }

    [HttpPost]
    public IActionResult Create([FromBody] CreateProductRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.Name) || request.Price<=0)
        {
            return BadRequest("Inavalid product data");
        }
        var Product = _service.Create(request);

        return Created("",Product);
    } 

    [HttpGet]
    public IActionResult GetAll()
    {
        var products = _service.GetAll();
        return Ok(products);
    } 

}