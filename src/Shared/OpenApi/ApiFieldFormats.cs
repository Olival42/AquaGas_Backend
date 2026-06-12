using System.Reflection;

namespace AquaGas.Shared.OpenApi;

/// <summary>
/// Regras de formato, enums válidos e exemplos alinhados às validações de domínio da API.
/// </summary>
public static class ApiFieldFormats
{
    public sealed record FieldRule(
        string? Description = null,
        string? Example = null,
        string? Pattern = null,
        string[]? AllowedValues = null,
        string? Format = null,
        double? Minimum = null,
        double? Maximum = null,
        int? MinLength = null,
        int? MaxLength = null,
        string? TypeExampleJson = null);

    public static FieldRule Resolve(PropertyInfo property, Type declaringType)
    {
        var attribute = property.GetCustomAttribute<OpenApiFieldAttribute>();
        if (attribute is not null)
            return FromAttribute(attribute);

        return ResolveByConvention(property.Name, declaringType.Name);
    }

    public static FieldRule? ResolveType(string typeName) =>
        TypeExamples.TryGetValue(typeName, out var rule) ? rule : null;

    public static FieldRule FromAttribute(OpenApiFieldAttribute attribute) =>
        new(
            Description: attribute.Description,
            Example: attribute.Example,
            Pattern: attribute.Pattern,
            AllowedValues: attribute.AllowedValues,
            Format: attribute.Format,
            Minimum: attribute.Minimum >= 0 ? attribute.Minimum : null,
            Maximum: attribute.Maximum >= 0 ? attribute.Maximum : null,
            MinLength: attribute.MinLength >= 0 ? attribute.MinLength : null,
            MaxLength: attribute.MaxLength >= 0 ? attribute.MaxLength : null);

    private static FieldRule ResolveByConvention(string propertyName, string declaringTypeName)
    {
        var key = propertyName.ToLowerInvariant();

        if (key == "reason")
        {
            if (declaringTypeName.Contains("Suspend", StringComparison.OrdinalIgnoreCase)
                || declaringTypeName.Contains("CancelPlan", StringComparison.OrdinalIgnoreCase)
                || declaringTypeName.Contains("Waive", StringComparison.OrdinalIgnoreCase)
                || declaringTypeName.Contains("CancelContract", StringComparison.OrdinalIgnoreCase))
            {
                return ReasonMediumRule;
            }

            return ReasonShortRule;
        }

        if (PropertyRules.TryGetValue(key, out var rule))
        {
            if (key == "type" && declaringTypeName.Contains("Product", StringComparison.OrdinalIgnoreCase))
                return ProductTypeRule;

            return rule;
        }

        if (declaringTypeName.Contains("Report", StringComparison.OrdinalIgnoreCase)
            && (key is "start" or "end"))
        {
            return DateTimeRule;
        }

        return Empty;
    }

    public static readonly FieldRule Empty = new();

    public static readonly FieldRule CpfRule = new(
        Description: "CPF com 11 dígitos. Aceita com ou sem máscara; será normalizado (somente números). Dígitos verificadores validados.",
        Example: "52998224725",
        Pattern: @"^\d{11}$",
        MinLength: 11,
        MaxLength: 14);

    public static readonly FieldRule CnpjRule = new(
        Description: "CNPJ com 14 dígitos. Aceita com ou sem máscara; será normalizado (somente números). Dígitos verificadores validados.",
        Example: "11222333000181",
        Pattern: @"^\d{14}$",
        MinLength: 14,
        MaxLength: 18);

    public static readonly FieldRule DocumentRule = new(
        Description: "CPF (11 dígitos) ou CNPJ (14 dígitos). Aceita formatação com pontos, traços e barras; será normalizado.",
        Example: "52998224725",
        Pattern: @"^(\d{11}|\d{14})$");

    public static readonly FieldRule PhoneRule = new(
        Description: "Telefone brasileiro com 10 ou 11 dígitos (DDD + número). Aceita máscara: (11) 99999-8888.",
        Example: "11999998888",
        Pattern: @"^\d{10,11}$",
        MinLength: 10,
        MaxLength: 15);

    public static readonly FieldRule EmailRule = new(
        Description: "E-mail válido. Convertido para minúsculas no cadastro.",
        Example: "contato@empresa.com.br",
        Format: "email",
        Pattern: @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static readonly FieldRule CepRule = new(
        Description: "CEP brasileiro com 8 dígitos. Aceita máscara 00000-000.",
        Example: "01001000",
        Pattern: @"^\d{8}$",
        MinLength: 8,
        MaxLength: 9);

    public static readonly FieldRule UserNameRule = new(
        Description: "Nome de usuário com 3 a 50 caracteres alfanuméricos (letras e números).",
        Example: "gerente01",
        Pattern: @"^[a-zA-Z0-9]{3,50}$",
        MinLength: 3,
        MaxLength: 50);

    public static readonly FieldRule PasswordRule = new(
        Description: "Mínimo 8 caracteres, com letra maiúscula, minúscula, número e caractere especial (@$!%*?&).",
        Example: "Senha@123",
        MinLength: 8);

    public static readonly FieldRule RoleRule = new(
        Description: "Perfil de acesso do usuário.",
        Example: "Manager",
        AllowedValues: ["Manager", "Employee"]);

    public static readonly FieldRule PlanCycleRule = new(
        Description: "Ciclo de cobrança do plano. Para `Custom`, informe também `durationInMonths` (2 a 60).",
        Example: "Monthly",
        AllowedValues: ["Monthly", "Quarterly", "Annual", "Custom"]);

    public static readonly FieldRule ProductTypeRule = new(
        Description: "Tipo do produto.",
        Example: "Gas",
        AllowedValues: ["Water", "Gas"]);

    public static readonly FieldRule StockMovementTypeRule = new(
        Description: "Tipo de movimentação de estoque.",
        Example: "Entry",
        AllowedValues: ["Entry", "Exit"]);

    public static readonly FieldRule UuidRule = new(
        Description: "Identificador único (UUID v4).",
        Example: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        Format: "uuid",
        Pattern: @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");

    public static readonly FieldRule DiscountRule = new(
        Description: "Percentual de desconto (opcional). Maior que 0 e no máximo 100.",
        Example: "5",
        Minimum: 0.01,
        Maximum: 100);

    public static readonly FieldRule DayOfMonthRule = new(
        Description: "Dia do mês (1 a 31).",
        Example: "10",
        Minimum: 1,
        Maximum: 31);

    public static readonly FieldRule DurationMonthsRule = new(
        Description: "Duração do contrato em meses. Obrigatório apenas quando `cycle` = Custom (entre 2 e 60).",
        Example: "12",
        Minimum: 2,
        Maximum: 60);

    public static readonly FieldRule PositiveQuantityRule = new(
        Description: "Quantidade inteira maior que zero.",
        Example: "1",
        Minimum: 1);

    public static readonly FieldRule NonNegativeQuantityRule = new(
        Description: "Quantidade inteira maior ou igual a zero.",
        Example: "10",
        Minimum: 0);

    public static readonly FieldRule PriceRule = new(
        Description: "Preço unitário maior que zero (até 2 casas decimais).",
        Example: "89.90",
        Minimum: 0.01);

    public static readonly FieldRule DateTimeRule = new(
        Description: "Data/hora em ISO 8601 (UTC recomendado).",
        Example: "2026-01-01T00:00:00Z",
        Format: "date-time");

    public static readonly FieldRule ReasonShortRule = new(
        Description: "Motivo ou observação (mínimo 3 caracteres).",
        Example: "Ajuste de estoque",
        MinLength: 3);

    public static readonly FieldRule ReasonMediumRule = new(
        Description: "Motivo (entre 5 e 500 caracteres).",
        Example: "Cliente solicitou suspensão temporária",
        MinLength: 5,
        MaxLength: 500);

    private static readonly Dictionary<string, FieldRule> PropertyRules =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["document"] = DocumentRule,
            ["cpf"] = CpfRule,
            ["cnpj"] = CnpjRule,
            ["phone"] = PhoneRule,
            ["email"] = EmailRule,
            ["cep"] = CepRule,
            ["username"] = UserNameRule,
            ["password"] = PasswordRule,
            ["newpassword"] = PasswordRule,
            ["role"] = RoleRule,
            ["cycle"] = PlanCycleRule,
            ["stockmovementtype"] = StockMovementTypeRule,
            ["customerid"] = UuidRule,
            ["productid"] = UuidRule,
            ["deliveryid"] = UuidRule,
            ["id"] = UuidRule,
            ["discount"] = DiscountRule,
            ["deliveryday"] = DayOfMonthRule,
            ["billingday"] = DayOfMonthRule,
            ["durationinmonths"] = DurationMonthsRule,
            ["quantity"] = PositiveQuantityRule,
            ["price"] = PriceRule,
            ["startdate"] = DateTimeRule,
            ["enddate"] = DateTimeRule,
            ["start"] = DateTimeRule,
            ["end"] = DateTimeRule,
            ["accesstoken"] = new FieldRule(
                Description: "Token JWT de acesso.",
                Example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."),
            ["expiresat"] = new FieldRule(
                Description: "Timestamp Unix (segundos) de expiração do access token.",
                Example: "1735689600"),
        };

    private static readonly Dictionary<string, FieldRule> TypeExamples =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["RegisterCustomerInput"] = new(
                Description: "Payload de cadastro de cliente com endereço.",
                TypeExampleJson: """
                {
                  "name": "Maria Silva",
                  "document": "529.982.247-25",
                  "email": "maria.silva@email.com",
                  "phone": "11999998888",
                  "address": {
                    "street": "Rua das Flores",
                    "neighborhood": "Centro",
                    "number": "100",
                    "complement": "Apto 12",
                    "city": "São Paulo",
                    "cep": "01001-000"
                  }
                }
                """),
            ["RegisterEmployeeInput"] = new(
                Description: "Cadastro de funcionário com usuário de acesso.",
                TypeExampleJson: """
                {
                  "user": {
                    "userName": "joao.vendas",
                    "password": "Senha@123",
                    "role": "Employee"
                  },
                  "employee": {
                    "name": "João Pereira",
                    "cpf": "52998224725",
                    "email": "joao@empresa.com",
                    "phone": "11988887777"
                  }
                }
                """),
            ["RegisterProductInput"] = new(
                Description: "Cadastro de produto com estoque inicial.",
                TypeExampleJson: """
                {
                  "name": "Botijão P13",
                  "type": "Gas",
                  "price": 89.90,
                  "quantity": 50
                }
                """),
            ["RegisterSaleInput"] = new(
                Description: "Registro de venda avulsa.",
                TypeExampleJson: """
                {
                  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "discount": 5,
                  "saleItems": [
                    { "productId": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "quantity": 2 }
                  ]
                }
                """),
            ["RegisterPlanInput"] = new(
                Description: "Cadastro de plano de assinatura.",
                TypeExampleJson: """
                {
                  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "cycle": "Monthly",
                  "discount": 0,
                  "deliveryDay": 10,
                  "billingDay": 5,
                  "durationInMonths": null,
                  "ignoreWarnings": false,
                  "items": [
                    { "productId": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "quantity": 1 }
                  ]
                }
                """),
            ["LoginInput"] = new(
                TypeExampleJson: """
                {
                  "userName": "gerente",
                  "password": "Senha@123"
                }
                """),
            ["UpdateStockInput"] = new(
                TypeExampleJson: """
                {
                  "stockMovementType": "Entry",
                  "quantity": 20,
                  "reason": "Reposição de estoque"
                }
                """),
            ["CustomerConsumptionHistoryInput"] = new(
                TypeExampleJson: """
                {
                  "startDate": "2026-01-01T00:00:00Z",
                  "endDate": "2026-05-31T23:59:59Z"
                }
                """),
            ["SalesReportInput"] = new(
                TypeExampleJson: """
                {
                  "start": "2026-01-01T00:00:00Z",
                  "end": "2026-05-31T23:59:59Z"
                }
                """),
            ["UpdateCustomerInput"] = new(
                Description: "Campos para atualização cadastral do cliente (valores opcionais).",
                TypeExampleJson: """
                {
                  "name": "Maria Silva Ramos",
                  "email": "maria.ramos@email.com",
                  "phone": "11999998888",
                  "address": {
                    "addressId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                    "street": "Avenida Paulista",
                    "neighborhood": "Bela Vista",
                    "number": "1000",
                    "complement": "Conjunto 50",
                    "city": "São Paulo",
                    "cep": "01310-100"
                  }
                }
                """),
            ["UpdateEmployeeInput"] = new(
                Description: "Campos para atualização cadastral do funcionário (valores opcionais).",
                TypeExampleJson: """
                {
                  "name": "João Pereira Lima",
                  "email": "joao.lima@empresa.com",
                  "phone": "11988887777",
                  "userName": "joao.vendas",
                  "newPassword": "NovaSenha@123",
                  "role": "Employee"
                }
                """),
            ["UpdateProductInput"] = new(
                Description: "Campos para atualização de produto (valores opcionais).",
                TypeExampleJson: """
                {
                  "name": "Botijão P13 Premium",
                  "price": 95.00,
                  "type": "Gas"
                }
                """),
            ["UpgradePlanInput"] = new(
                Description: "Payload para upgrade do plano de assinatura.",
                TypeExampleJson: """
                {
                  "cycle": "Annual",
                  "durationInMonths": 12,
                  "reason": "Upgrade de plano para ciclo anual corporativo",
                  "items": [
                    { "productId": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "quantity": 2 }
                  ]
                }
                """),
            ["DowngradePlanInput"] = new(
                Description: "Payload para downgrade do plano de assinatura.",
                TypeExampleJson: """
                {
                  "cycle": "Monthly",
                  "durationInMonths": null,
                  "reason": "Redução da quantidade mensal consumida pelo cliente",
                  "items": [
                    { "productId": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "quantity": 1 }
                  ]
                }
                """),
            ["RescheduleDeliveryInput"] = new(
                Description: "Payload para reagendar uma entrega do plano.",
                TypeExampleJson: """
                {
                  "deliveryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "newDate": "2026-06-15T09:00:00Z",
                  "reason": "Cliente solicitou reagendamento por ausência no local"
                }
                """),
        };
}
