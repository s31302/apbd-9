using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<int> DoSomethingAsync(WarehouseDTO warehouse)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            if (warehouse.Amount <= 0)
            {
                throw new Exception("Amount must be bigger than 0");
            }
            
            //czy product istnieje
            command.CommandText = "Select Product.IdProduct From Product where Product.IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);

            var exists = await command.ExecuteScalarAsync();
            if (exists == null)
            {
                throw new Exception("Product not exists");
            }

            command.Parameters.Clear();
            
            //czy magazyn istnieje
            command.CommandText = "Select IdWarehouse From Warehouse where IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
            exists = await command.ExecuteScalarAsync();
            if (exists == null)
            {
                throw new Exception("Warehouse not exists");
            }
            
            command.Parameters.Clear();

            if (warehouse.CreatedAt >= DateTime.Now){
                throw new Exception("AtCreated is wrong");
            }
                
            //sprawdzamy, czy w tabeli Order istnieje rekord z IdProduktu i Ilością (Amount)
            command.CommandText =
                    "Select [Order].IdOrder From [Order] join Product on Product.IdProduct = [Order].IdProduct where Product.IdProduct = @IdProduct and Amount = @Amount";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);

            var idOrder = await command.ExecuteScalarAsync();
            if (idOrder == null)
            {
                throw new Exception("Order not exists");
            }
                
            command.Parameters.Clear();

            //czy nie zostalo zrealizowane 
            command.CommandText = "SELECT Product_Warehouse.IdOrder FROM Product_Warehouse JOIN [Order] ON Product_Warehouse.IdOrder = [Order].IdOrder JOIN Product ON Product.IdProduct = [Order].IdProduct WHERE Product.IdProduct = @IdProduct AND Product_Warehouse.Amount = @Amount";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            exists = await command.ExecuteScalarAsync();
            if (exists != null)
            {
                throw new Exception("Order is already completed");
            }
            command.Parameters.Clear();
            
            //aktualzacja zamwienia
            command.CommandText = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE [Order].IdProduct = @IdProduct and Amount = @Amount";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            
            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();
            
            //cena
            command.CommandText = "SELECT Product.Price * Amount FROM Product join [Order] on Product.IdProduct = [Order].IdProduct WHERE Product.IdProduct = @IdProduct and [Order].Amount = @Amount";
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            
            var price = await command.ExecuteScalarAsync();
            command.Parameters.Clear();
            
            command.CommandText = "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) OUTPUT INSERTED.IdProductWarehouse VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
            command.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
            command.Parameters.AddWithValue("@IdOrder", Convert.ToInt32(idOrder));
            command.Parameters.AddWithValue("@Price", Convert.ToDecimal(price));
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            command.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            var id = await command.ExecuteScalarAsync();
            
            
            await transaction.CommitAsync(); 
            
            return Convert.ToInt32(id);
                
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }
}