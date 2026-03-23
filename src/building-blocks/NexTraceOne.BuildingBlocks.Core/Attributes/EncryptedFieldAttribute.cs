namespace NexTraceOne.BuildingBlocks.Core.Attributes;

/// <summary>
/// Marca uma propriedade string como campo sensível que deve ser encriptado em repouso (at-rest).
/// O NexTraceDbContextBase aplica automaticamente o EncryptedStringConverter (AES-256-GCM)
/// a todas as propriedades anotadas com este atributo durante OnModelCreating.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class EncryptedFieldAttribute : Attribute;
