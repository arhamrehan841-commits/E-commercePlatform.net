using MyApi.Models;

namespace MyApi.Services;

public interface IProductService
{

    Product Create(CreateProductRequest request);

    List<Product> GetAll();

}