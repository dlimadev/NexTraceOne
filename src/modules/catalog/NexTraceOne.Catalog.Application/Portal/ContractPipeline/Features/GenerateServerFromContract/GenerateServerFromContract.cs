using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;

namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateServerFromContract;

/// <summary>Gera stubs de servidor a partir de um contrato OpenAPI.</summary>
public static class GenerateServerFromContract
{
    /// <summary>Comando para gerar código de servidor a partir de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string TargetLanguage,
        string ServiceName) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] SupportedLanguages = ["dotnet", "java", "nodejs", "go", "python"];

        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetLanguage)
                .NotEmpty()
                .Must(l => SupportedLanguages.Contains(l.ToLowerInvariant()))
                .WithMessage("TargetLanguage must be one of: dotnet, java, nodejs, go, python.");
        }
    }

    /// <summary>Handler que gera stubs de servidor na linguagem alvo.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var files = request.TargetLanguage.ToLowerInvariant() switch
            {
                "dotnet" => GenerateDotNetServerStubs(request.ServiceName),
                "java" => GenerateJavaServerStubs(request.ServiceName),
                "nodejs" => GenerateNodeJsServerStubs(request.ServiceName),
                "go" => GenerateGoServerStubs(request.ServiceName),
                "python" => GeneratePythonServerStubs(request.ServiceName),
                _ => new List<GeneratedFile>()
            };

            return Task.FromResult(Result<Response>.Success(new Response(
                ContractVersionId: request.ContractVersionId,
                TargetLanguage: request.TargetLanguage,
                Files: files)));
        }

        private static List<GeneratedFile> GenerateDotNetServerStubs(string serviceName)
        {
            var controllerName = $"{serviceName}Controller";
            var routePrefix = serviceName.ToLowerInvariant();
            var serviceInterface = $"I{serviceName}Service";
            var projectName = $"{serviceName}.API";
            return
            [
                new GeneratedFile(
                    $"Controllers/{controllerName}.cs",
                    $$"""
                    using Microsoft.AspNetCore.Mvc;

                    namespace {{projectName}}.Controllers;

                    /// <summary>Controller gerado pelo NexTraceOne Contract-to-Code Pipeline.</summary>
                    [ApiController]
                    [Route("api/v1/{{routePrefix}}")]
                    public sealed class {{controllerName}} : ControllerBase
                    {
                        private readonly {{serviceInterface}} _service;

                        public {{controllerName}}({{serviceInterface}} service) => _service = service;

                        [HttpGet]
                        [ProducesResponseType(StatusCodes.Status200OK)]
                        public async Task<IActionResult> GetAllAsync(CancellationToken ct)
                            => Ok(await _service.ListAsync(ct));

                        [HttpGet("{id:guid}")]
                        [ProducesResponseType(StatusCodes.Status200OK)]
                        [ProducesResponseType(StatusCodes.Status404NotFound)]
                        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
                        {
                            var item = await _service.GetByIdAsync(id, ct);
                            return item is null ? NotFound() : Ok(item);
                        }

                        [HttpPost]
                        [ProducesResponseType(StatusCodes.Status201Created)]
                        [ProducesResponseType(StatusCodes.Status400BadRequest)]
                        public async Task<IActionResult> CreateAsync([FromBody] object request, CancellationToken ct)
                        {
                            var created = await _service.CreateAsync(request, ct);
                            return Created($"/api/v1/{{routePrefix}}/{created}", created);
                        }

                        [HttpPut("{id:guid}")]
                        [ProducesResponseType(StatusCodes.Status200OK)]
                        [ProducesResponseType(StatusCodes.Status404NotFound)]
                        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] object request, CancellationToken ct)
                        {
                            var updated = await _service.UpdateAsync(id, request, ct);
                            return updated is null ? NotFound() : Ok(updated);
                        }

                        [HttpDelete("{id:guid}")]
                        [ProducesResponseType(StatusCodes.Status204NoContent)]
                        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
                        {
                            await _service.DeleteAsync(id, ct);
                            return NoContent();
                        }
                    }
                    """,
                    "csharp",
                    $"ASP.NET Core controller stub for {serviceName}"),

                new GeneratedFile(
                    $"Services/{serviceInterface}.cs",
                    $$"""
                    namespace {{projectName}}.Services;

                    /// <summary>Contrato do serviço de aplicação gerado pelo NexTraceOne Pipeline.</summary>
                    public interface {{serviceInterface}}
                    {
                        Task<IEnumerable<object>> ListAsync(CancellationToken ct = default);
                        Task<object?> GetByIdAsync(Guid id, CancellationToken ct = default);
                        Task<object> CreateAsync(object request, CancellationToken ct = default);
                        Task<object?> UpdateAsync(Guid id, object request, CancellationToken ct = default);
                        Task DeleteAsync(Guid id, CancellationToken ct = default);
                    }
                    """,
                    "csharp",
                    $"Service interface for {serviceName}"),

                new GeneratedFile(
                    $"{serviceName}.API.csproj",
                    $$"""
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <AssemblyName>{{projectName}}</AssemblyName>
                      </PropertyGroup>
                      <ItemGroup>
                        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.*" />
                      </ItemGroup>
                    </Project>
                    """,
                    "xml",
                    $".NET project file for {serviceName}")
            ];
        }

        private static List<GeneratedFile> GenerateJavaServerStubs(string serviceName)
        {
            var lowerName = serviceName.ToLower();
            return
            [
                new GeneratedFile(
                    $"src/main/java/com/example/{lowerName}/{serviceName}Controller.java",
                    $$"""
                    package com.example.{{lowerName}};

                    import com.example.{{lowerName}}.service.{{serviceName}}Service;
                    import org.springframework.http.ResponseEntity;
                    import org.springframework.web.bind.annotation.*;
                    import java.net.URI;
                    import java.util.List;
                    import java.util.UUID;

                    /** Controller gerado pelo NexTraceOne Contract-to-Code Pipeline. */
                    @RestController
                    @RequestMapping("/api/v1/{{lowerName}}")
                    public class {{serviceName}}Controller {

                        private final {{serviceName}}Service service;

                        public {{serviceName}}Controller({{serviceName}}Service service) {
                            this.service = service;
                        }

                        @GetMapping
                        public ResponseEntity<List<Object>> getAll() {
                            return ResponseEntity.ok(service.findAll());
                        }

                        @GetMapping("/{id}")
                        public ResponseEntity<Object> getById(@PathVariable UUID id) {
                            return service.findById(id)
                                .map(ResponseEntity::ok)
                                .orElse(ResponseEntity.notFound().build());
                        }

                        @PostMapping
                        public ResponseEntity<Object> create(@RequestBody Object request) {
                            Object created = service.create(request);
                            return ResponseEntity.created(URI.create("/api/v1/{{lowerName}}")).body(created);
                        }

                        @PutMapping("/{id}")
                        public ResponseEntity<Object> update(@PathVariable UUID id, @RequestBody Object request) {
                            return service.update(id, request)
                                .map(ResponseEntity::ok)
                                .orElse(ResponseEntity.notFound().build());
                        }

                        @DeleteMapping("/{id}")
                        public ResponseEntity<Void> delete(@PathVariable UUID id) {
                            service.delete(id);
                            return ResponseEntity.noContent().build();
                        }
                    }
                    """,
                    "java",
                    $"Spring Boot controller stub for {serviceName}"),

                new GeneratedFile(
                    $"src/main/java/com/example/{lowerName}/service/{serviceName}Service.java",
                    $$"""
                    package com.example.{{lowerName}}.service;

                    import java.util.List;
                    import java.util.Optional;
                    import java.util.UUID;

                    /** Contrato do serviço gerado pelo NexTraceOne Pipeline. */
                    public interface {{serviceName}}Service {
                        List<Object> findAll();
                        Optional<Object> findById(UUID id);
                        Object create(Object request);
                        Optional<Object> update(UUID id, Object request);
                        void delete(UUID id);
                    }
                    """,
                    "java",
                    $"Spring Boot service interface for {serviceName}"),

                new GeneratedFile(
                    "pom.xml",
                    $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <project xmlns="http://maven.apache.org/POM/4.0.0"
                             xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                             xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
                      <modelVersion>4.0.0</modelVersion>
                      <parent>
                        <groupId>org.springframework.boot</groupId>
                        <artifactId>spring-boot-starter-parent</artifactId>
                        <version>3.4.0</version>
                      </parent>
                      <groupId>com.example</groupId>
                      <artifactId>{{lowerName}}</artifactId>
                      <version>0.0.1-SNAPSHOT</version>
                      <name>{{serviceName}}</name>
                      <description>Generated by NexTraceOne Contract-to-Code Pipeline</description>
                      <dependencies>
                        <dependency>
                          <groupId>org.springframework.boot</groupId>
                          <artifactId>spring-boot-starter-web</artifactId>
                        </dependency>
                        <dependency>
                          <groupId>org.springframework.boot</groupId>
                          <artifactId>spring-boot-starter-validation</artifactId>
                        </dependency>
                        <dependency>
                          <groupId>org.springframework.boot</groupId>
                          <artifactId>spring-boot-starter-test</artifactId>
                          <scope>test</scope>
                        </dependency>
                      </dependencies>
                      <build>
                        <plugins>
                          <plugin>
                            <groupId>org.springframework.boot</groupId>
                            <artifactId>spring-boot-maven-plugin</artifactId>
                          </plugin>
                        </plugins>
                      </build>
                    </project>
                    """,
                    "xml",
                    $"Maven POM for {serviceName}")
            ];
        }

        private static List<GeneratedFile> GenerateNodeJsServerStubs(string serviceName)
        {
            var lowerName = serviceName.ToLower();
            return
            [
                new GeneratedFile(
                    $"src/routes/{lowerName}.js",
                    $$"""
                    'use strict';

                    const express = require('express');
                    const { v4: uuidv4 } = require('uuid');
                    const router = express.Router();

                    // Generated by NexTraceOne Contract-to-Code Pipeline

                    const store = new Map();

                    router.get('/', (_req, res) => {
                        res.json([...store.values()]);
                    });

                    router.get('/:id', (req, res) => {
                        const item = store.get(req.params.id);
                        if (!item) return res.status(404).json({ error: 'Not found' });
                        res.json(item);
                    });

                    router.post('/', (req, res) => {
                        const id = uuidv4();
                        const item = { id, ...req.body, createdAt: new Date().toISOString() };
                        store.set(id, item);
                        res.status(201).location(`/api/v1/{{lowerName}}/${id}`).json(item);
                    });

                    router.put('/:id', (req, res) => {
                        if (!store.has(req.params.id)) return res.status(404).json({ error: 'Not found' });
                        const item = { ...store.get(req.params.id), ...req.body, updatedAt: new Date().toISOString() };
                        store.set(req.params.id, item);
                        res.json(item);
                    });

                    router.delete('/:id', (req, res) => {
                        store.delete(req.params.id);
                        res.status(204).send();
                    });

                    module.exports = router;
                    """,
                    "javascript",
                    $"Express.js router stub for {serviceName}"),

                new GeneratedFile(
                    "package.json",
                    $$"""
                    {
                      "name": "{{lowerName}}",
                      "version": "0.1.0",
                      "description": "Generated by NexTraceOne Contract-to-Code Pipeline",
                      "main": "src/app.js",
                      "scripts": {
                        "start": "node src/app.js",
                        "dev": "nodemon src/app.js",
                        "test": "jest"
                      },
                      "dependencies": {
                        "express": "^4.21.0",
                        "uuid": "^11.0.0"
                      },
                      "devDependencies": {
                        "nodemon": "^3.1.0",
                        "jest": "^29.7.0",
                        "supertest": "^7.0.0"
                      }
                    }
                    """,
                    "json",
                    $"npm package.json for {serviceName}"),

                new GeneratedFile(
                    "src/app.js",
                    $$"""
                    'use strict';

                    const express = require('express');
                    const {{lowerName}}Router = require('./routes/{{lowerName}}');

                    const app = express();
                    app.use(express.json());
                    app.use('/api/v1/{{lowerName}}', {{lowerName}}Router);

                    const PORT = process.env.PORT || 3000;
                    app.listen(PORT, () => console.log(`{{serviceName}} service running on port ${PORT}`));

                    module.exports = app;
                    """,
                    "javascript",
                    $"Express.js app entrypoint for {serviceName}")
            ];
        }

        private static List<GeneratedFile> GenerateGoServerStubs(string serviceName)
        {
            var lowerName = serviceName.ToLower();
            return
            [
                new GeneratedFile(
                    $"internal/{lowerName}/handler.go",
                    $$"""
                    package {{lowerName}}

                    import (
                        "encoding/json"
                        "net/http"

                        "github.com/google/uuid"
                    )

                    // Handler gerado pelo NexTraceOne Contract-to-Code Pipeline.

                    var store = make(map[string]map[string]any)

                    func HandleGetAll(w http.ResponseWriter, r *http.Request) {
                        items := make([]map[string]any, 0, len(store))
                        for _, v := range store {
                            items = append(items, v)
                        }
                        w.Header().Set("Content-Type", "application/json")
                        json.NewEncoder(w).Encode(items)
                    }

                    func HandleGetByID(w http.ResponseWriter, r *http.Request) {
                        id := r.PathValue("id")
                        item, ok := store[id]
                        if !ok {
                            http.Error(w, `{"error":"not found"}`, http.StatusNotFound)
                            return
                        }
                        w.Header().Set("Content-Type", "application/json")
                        json.NewEncoder(w).Encode(item)
                    }

                    func HandleCreate(w http.ResponseWriter, r *http.Request) {
                        var body map[string]any
                        if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
                            http.Error(w, `{"error":"invalid body"}`, http.StatusBadRequest)
                            return
                        }
                        id := uuid.NewString()
                        body["id"] = id
                        store[id] = body
                        w.Header().Set("Content-Type", "application/json")
                        w.Header().Set("Location", "/api/v1/{{lowerName}}/"+id)
                        w.WriteHeader(http.StatusCreated)
                        json.NewEncoder(w).Encode(body)
                    }

                    func HandleDelete(w http.ResponseWriter, r *http.Request) {
                        id := r.PathValue("id")
                        delete(store, id)
                        w.WriteHeader(http.StatusNoContent)
                    }
                    """,
                    "go",
                    $"Go handler stub for {serviceName}"),

                new GeneratedFile(
                    "go.mod",
                    $$"""
                    module github.com/example/{{lowerName}}

                    go 1.23

                    require github.com/google/uuid v1.6.0
                    """,
                    "go",
                    $"Go module file for {serviceName}"),

                new GeneratedFile(
                    "cmd/server/main.go",
                    $$"""
                    package main

                    import (
                        "log"
                        "net/http"

                        "github.com/example/{{lowerName}}/internal/{{lowerName}}"
                    )

                    // Generated by NexTraceOne Contract-to-Code Pipeline.
                    func main() {
                        mux := http.NewServeMux()
                        mux.HandleFunc("GET /api/v1/{{lowerName}}", {{lowerName}}.HandleGetAll)
                        mux.HandleFunc("GET /api/v1/{{lowerName}}/{id}", {{lowerName}}.HandleGetByID)
                        mux.HandleFunc("POST /api/v1/{{lowerName}}", {{lowerName}}.HandleCreate)
                        mux.HandleFunc("DELETE /api/v1/{{lowerName}}/{id}", {{lowerName}}.HandleDelete)

                        log.Println("{{serviceName}} listening on :8080")
                        log.Fatal(http.ListenAndServe(":8080", mux))
                    }
                    """,
                    "go",
                    $"Go main entrypoint for {serviceName}")
            ];
        }

        private static List<GeneratedFile> GeneratePythonServerStubs(string serviceName)
        {
            var lowerName = serviceName.ToLower();
            return
            [
                new GeneratedFile(
                    $"{lowerName}/routes.py",
                    $$"""
                    from __future__ import annotations

                    from typing import Any
                    from uuid import UUID, uuid4

                    from fastapi import APIRouter, HTTPException, status

                    # Generated by NexTraceOne Contract-to-Code Pipeline
                    router = APIRouter(prefix="/api/v1/{{lowerName}}", tags=["{{serviceName}}"])

                    _store: dict[str, dict[str, Any]] = {}


                    @router.get("/", response_model=list[dict[str, Any]])
                    async def list_items() -> list[dict[str, Any]]:
                        return list(_store.values())


                    @router.get("/{item_id}", response_model=dict[str, Any])
                    async def get_item(item_id: UUID) -> dict[str, Any]:
                        item = _store.get(str(item_id))
                        if item is None:
                            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Not found")
                        return item


                    @router.post("/", status_code=status.HTTP_201_CREATED, response_model=dict[str, Any])
                    async def create_item(body: dict[str, Any]) -> dict[str, Any]:
                        item_id = str(uuid4())
                        item = {"id": item_id, **body}
                        _store[item_id] = item
                        return item


                    @router.put("/{item_id}", response_model=dict[str, Any])
                    async def update_item(item_id: UUID, body: dict[str, Any]) -> dict[str, Any]:
                        key = str(item_id)
                        if key not in _store:
                            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Not found")
                        _store[key] = {**_store[key], **body}
                        return _store[key]


                    @router.delete("/{item_id}", status_code=status.HTTP_204_NO_CONTENT)
                    async def delete_item(item_id: UUID) -> None:
                        _store.pop(str(item_id), None)
                    """,
                    "python",
                    $"FastAPI router stub for {serviceName}"),

                new GeneratedFile(
                    $"{lowerName}/main.py",
                    $$"""
                    from fastapi import FastAPI

                    from .routes import router

                    # Generated by NexTraceOne Contract-to-Code Pipeline
                    app = FastAPI(title="{{serviceName}}", version="0.1.0")
                    app.include_router(router)
                    """,
                    "python",
                    $"FastAPI application entrypoint for {serviceName}"),

                new GeneratedFile(
                    "pyproject.toml",
                    $$"""
                    [build-system]
                    requires = ["hatchling"]
                    build-backend = "hatchling.build"

                    [project]
                    name = "{{lowerName}}"
                    version = "0.1.0"
                    description = "Generated by NexTraceOne Contract-to-Code Pipeline"
                    requires-python = ">=3.12"
                    dependencies = [
                        "fastapi>=0.115.0",
                        "uvicorn[standard]>=0.32.0",
                    ]

                    [project.scripts]
                    serve = "{{lowerName}}.main:app"
                    """,
                    "toml",
                    $"Python project config for {serviceName}")
            ];
        }
    }

    /// <summary>Resposta com ficheiros de servidor gerados.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string TargetLanguage,
        IReadOnlyList<GeneratedFile> Files);
}
