using NailToolInventory.Models;
using Xunit;

namespace NailToolInventory.Tests;

public class ProductTests
{
    [Fact]
    public void NewProduct_ShouldStartWithZeroInventory()
    {
        // Arrange + Act
        var product = CreateProduct();

        // Assert
        Assert.Equal(0, product.QuantityOnHand);
    }


    [Fact]
    public void ReceiveStock_WithPositiveQuantity_ShouldIncreaseInventory()
    {
        var product = CreateProduct();

        product.ReceiveStock(10);

        Assert.Equal(10, product.QuantityOnHand);
    }


    [Fact]
    public void IssueStock_WhenInventoryIsEnough_ShouldDecreaseInventory()
    {
        var product = CreateProduct();
        product.ReceiveStock(10);

        product.IssueStock(4);

        Assert.Equal(6, product.QuantityOnHand);
    }


    [Fact]
    public void IssueStock_WhenInventoryIsNotEnough_ShouldThrowException()
    {
        var product = CreateProduct();
        product.ReceiveStock(5);

        Assert.Throws<InvalidOperationException>(
            () => product.IssueStock(6));

        Assert.Equal(5, product.QuantityOnHand);
    }


    [Fact]
    public void IsLowStock_ShouldUseReorderLevel()
    {
        var product = CreateProduct();

        product.ReceiveStock(5);
        Assert.True(product.IsLowStock());

        product.ReceiveStock(1);
        Assert.False(product.IsLowStock());
    }


    private static Product CreateProduct()
    {
        return new Product(
            sku: "A-001",
            name: "A-Shape Nipper",
            category: ProductCategory.AShapeNipper,
            costPrice: 8.50m,
            sellingPrice: 15.00m,
            reorderLevel: 5);
    }

    [Fact]
    public void ReceiveStock_WhenProductIsInactive_ShouldThrow()
    {
        // Arrange
        var product = CreateProduct();
        product.Deactivate();

        // Act
        var action = () => product.ReceiveStock(5);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void IssueStock_WhenProductIsInactive_ShouldThrow()
    {
        // Arrange
        var product = CreateProduct();
        product.ReceiveStock(10);
        product.Deactivate();

        // Act
        var action = () => product.IssueStock(2);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }
}