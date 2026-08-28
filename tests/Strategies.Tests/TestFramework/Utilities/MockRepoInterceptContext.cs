// <copyright file="MockRepoInterceptContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Utilities;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Repositories.Pagination;

public class MockRepoInterceptContext<TEntity>
{
    public int GetCallCount { get; private set; }

    public int GetListCallCount { get; private set; }

    public int GetPagedCallCount { get; private set; }

    public int CreateCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int TotalCallCount { get; private set; }

    public List<TEntity?> GetResultLog { get; } = [];

    public List<List<TEntity>?> GetListResultLog { get; } = [];

    public List<PagedEntities<TEntity>?> GetPagedResultLog { get; } = [];

    public List<TEntity?> CreateResultLog { get; } = [];

    public List<TEntity?> UpdateResultLog { get; } = [];

    public TEntity? LastGetResult
    {
        get;

        set
        {
            field = value;
            GetCallCount++;
            TotalCallCount++;
            GetResultLog.Add(value);
        }
    }

    public List<TEntity>? LastGetListResult
    {
        get;

        set
        {
            field = value;
            GetListCallCount++;
            TotalCallCount++;
            GetListResultLog.Add(value);
        }
    }

    public PagedEntities<TEntity>? LastGetPagedResult
    {
        get;

        set
        {
            field = value;
            GetPagedCallCount++;
            TotalCallCount++;
            GetPagedResultLog.Add(value);
        }
    }

    public TEntity? LastCreateResult
    {
        get;

        set
        {
            field = value;
            CreateCallCount++;
            TotalCallCount++;
            CreateResultLog.Add(value);
        }
    }

    public TEntity? LastUpdateResult
    {
        get;

        set
        {
            field = value;
            UpdateCallCount++;
            TotalCallCount++;
            UpdateResultLog.Add(value);
        }
    }
}
