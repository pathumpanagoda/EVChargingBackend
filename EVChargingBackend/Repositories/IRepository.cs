/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Generic repository interface for data access operations
 */

using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <returns>Collection of entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Gets entities matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching entities</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter);

    /// <summary>
    /// Gets the first entity matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching entity if found, null otherwise</returns>
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter);

    /// <summary>
    /// Counts entities matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching entities</returns>
    Task<long> CountAsync(Expression<Func<T, bool>> filter);

    /// <summary>
    /// Creates a new entity
    /// </summary>
    /// <param name="entity">Entity to create</param>
    /// <returns>Created entity</returns>
    Task<T> CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="entity">Updated entity</param>
    /// <returns>Updated entity if found, null otherwise</returns>
    Task<T?> UpdateAsync(string id, T entity);

    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity exists with the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
}