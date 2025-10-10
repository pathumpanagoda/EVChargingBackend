/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Generic repository interface for data access operations
 */

using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;


/// Generic repository interface for CRUD operations

/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    
    /// Gets an entity by its ID
    
    /// <param name="id">Entity ID</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(string id);

    
    /// Gets all entities
    
    /// <returns>Collection of entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    
    /// Gets entities matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching entities</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter);

    
    /// Gets the first entity matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching entity if found, null otherwise</returns>
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter);

    
    /// Counts entities matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching entities</returns>
    Task<long> CountAsync(Expression<Func<T, bool>> filter);

    
    /// Creates a new entity
    
    /// <param name="entity">Entity to create</param>
    /// <returns>Created entity</returns>
    Task<T> CreateAsync(T entity);

    
    /// Updates an existing entity
    
    /// <param name="id">Entity ID</param>
    /// <param name="entity">Updated entity</param>
    /// <returns>Updated entity if found, null otherwise</returns>
    Task<T?> UpdateAsync(string id, T entity);

    
    /// Deletes an entity by ID
    
    /// <param name="id">Entity ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id);

    
    /// Checks if an entity exists with the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
}