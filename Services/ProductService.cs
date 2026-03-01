using MyApi.Models;

namespace MyApi.Services;

public class ProductService : IProductService
{

    private static List<Product> _products = new();

    private static int _id = 1;
    public Product Create(CreateProductRequest request)
    {
        var Productobj = new Product
        {
            Id = _id++,
            Name = request.Name,
            Price = request.Price
        };

        _products.Add(Productobj);

        return Productobj;
    }

    public List<Product> GetAll()
    {
        return _products;
    }
}