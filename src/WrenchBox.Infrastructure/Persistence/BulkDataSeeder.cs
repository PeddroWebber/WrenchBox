using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Infrastructure.Persistence;

internal static class BulkDataSeeder
{
    internal const int CustomerCount = 100;
    internal const int WorkOrderCount = 300;

    private static readonly string[] FirstNames =
    [
        "João", "Maria", "Pedro", "Ana", "Carlos", "Juliana", "Rafael", "Fernanda", "Lucas", "Camila",
        "Bruno", "Larissa", "Diego", "Patrícia", "Gabriel", "Aline", "Felipe", "Mariana", "Rodrigo", "Beatriz"
    ];

    private static readonly string[] LastNames =
    [
        "Silva", "Santos", "Oliveira", "Souza", "Lima", "Ferreira", "Almeida", "Costa", "Ribeiro", "Carvalho",
        "Gomes", "Martins", "Araújo", "Melo", "Barbosa", "Pereira", "Nascimento", "Rocha", "Dias", "Castro"
    ];

    private static readonly (string Brand, string Model)[] VehicleCatalog =
    [
        ("Toyota", "Corolla"), ("Honda", "Civic"), ("Volkswagen", "Gol"), ("Fiat", "Argo"),
        ("Chevrolet", "Onix"), ("Hyundai", "HB20"), ("Jeep", "Compass"), ("Renault", "Kwid"),
        ("Ford", "Ka"), ("Nissan", "Kicks"), ("Peugeot", "208"), ("Citroën", "C3")
    ];

    internal static async Task SeedAsync(
        WrenchBoxDbContext context,
        ILogger logger,
        bool useLightSeed = false,
        CancellationToken cancellationToken = default)
    {
        if (await context.Services.AnyAsync(cancellationToken))
            return;

        var customerCount = useLightSeed ? 5 : CustomerCount;
        var workOrderCount = useLightSeed ? 6 : WorkOrderCount;

        logger.LogInformation("Gerando dados de demonstração ({Customers} clientes, {WorkOrders} ordens de serviço)...",
            customerCount, workOrderCount);

        var services = CreateServices();
        await context.Services.AddRangeAsync(services, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Criados {Count} serviços", services.Count);

        var parts = CreateParts();
        await context.Parts.AddRangeAsync(parts, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Criadas {Count} peças com movimentações de estoque", parts.Count);

        var customers = CreateCustomers(customerCount);
        await context.Customers.AddRangeAsync(customers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Criados {Count} clientes com {VehicleCount} veículos",
            customers.Count, customers.Sum(c => c.Vehicles.Count));

        context.ChangeTracker.Clear();

        var catalogServices = await context.Services.AsNoTracking().ToListAsync(cancellationToken);
        var catalogParts = await context.Parts.AsNoTracking().ToListAsync(cancellationToken);
        var catalogCustomers = await context.Customers
            .AsNoTracking()
            .Include(c => c.Vehicles)
            .ToListAsync(cancellationToken);
        var partsById = catalogParts.ToDictionary(p => p.Id);

        var workOrders = new List<WorkOrder>(workOrderCount);
        for (var i = 0; i < workOrderCount; i++)
        {
            workOrders.Add(CreateWorkOrder(i, catalogCustomers, catalogServices, catalogParts, partsById));

            if (workOrders.Count >= 50)
            {
                await context.WorkOrders.AddRangeAsync(workOrders, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                workOrders.Clear();
            }
        }

        if (workOrders.Count > 0)
        {
            await context.WorkOrders.AddRangeAsync(workOrders, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Criadas {Count} ordens de serviço em todos os status", workOrderCount);
    }

    private static List<Service> CreateServices() =>
    [
        Service.Create("Troca de Óleo", "Troca completa de óleo sintético", 150m, 60),
        Service.Create("Alinhamento", "Alinhamento das quatro rodas", 200m, 90),
        Service.Create("Inspeção de Freios", "Inspeção completa do sistema de freios", 120m, 45),
        Service.Create("Troca de Pastilhas Dianteiras", "Substituição das pastilhas de freio dianteiras", 280m, 120),
        Service.Create("Rodízio de Pneus", "Rodízio e balanceamento de todos os pneus", 80m, 40),
        Service.Create("Troca de Filtro de Ar", "Substituição do filtro de ar do motor", 90m, 30),
        Service.Create("Teste de Bateria", "Verificação de saúde e carga da bateria", 60m, 25),
        Service.Create("Carga de Ar-Condicionado", "Recarga de gás do ar-condicionado", 220m, 75),
        Service.Create("Troca de Velas", "Substituição do jogo de velas de ignição", 175m, 55),
        Service.Create("Troca de Fluido de Câmbio", "Serviço de fluido de câmbio automático", 320m, 90),
        Service.Create("Flush do Radiador", "Limpeza e reposição do fluido do radiador", 190m, 60),
        Service.Create("Inspeção de Suspensão", "Verificação de amortecedores e buchas", 130m, 50),
        Service.Create("Inspeção do Escapamento", "Inspeção de vazamentos no escapamento", 110m, 45),
        Service.Create("Limpeza do Sistema de Combustível", "Limpeza de bicos e admissão", 250m, 80),
        Service.Create("Inspeção da Correia Dentada", "Avaliação de desgaste da correia dentada", 140m, 40),
        Service.Create("Regulagem de Embreagem", "Ajuste do pedal e cabo de embreagem", 160m, 70),
        Service.Create("Serviço de Direção Hidráulica", "Troca de fluido e verificação de mangueiras", 180m, 55),
        Service.Create("Restauração de Faróis", "Polimento e restauração das lentes dos faróis", 95m, 35),
        Service.Create("Troca de Palhetas", "Substituição das palhetas dianteiras", 70m, 15),
        Service.Create("Inspeção Pré-Compra", "Laudo completo de condição do veículo", 350m, 120),
        Service.Create("Diagnóstico do Motor", "Análise de códigos de falha via scanner OBD", 100m, 30),
        Service.Create("Antioxidante", "Tratamento anticorrosivo do assoalho", 400m, 150),
        Service.Create("Vitrificação Cerâmica", "Proteção cerâmica da pintura", 800m, 240),
        Service.Create("Estética Básica", "Lavagem básica interna e externa", 120m, 90),
        Service.Create("Estética Premium", "Detalhamento completo com correção de pintura", 450m, 300),
        Service.Create("Manutenção de Frota", "Pacote padrão de serviço para veículos de frota", 500m, 180),
        Service.Create("Troca de Óleo de Moto", "Troca de óleo para motocicletas", 90m, 35),
        Service.Create("Troca de Filtro Diesel", "Substituição do filtro de combustível diesel", 210m, 50),
        Service.Create("Inspeção do Turbo", "Inspeção visual do turbo compressor", 170m, 45),
        Service.Create("Diagnóstico Elétrico", "Localização de falhas no sistema elétrico", 200m, 90)
    ];

    private static List<Part> CreateParts()
    {
        var definitions = new (string Name, string Sku, decimal Price, int Stock, int Min)[]
        {
            ("Óleo Sintético 5W30", "OIL-5W30", 45m, 500, 50),
            ("Óleo Sintético 5W40", "OIL-5W40", 52m, 400, 40),
            ("Filtro de Óleo", "FLT-OIL-001", 25m, 350, 30),
            ("Filtro de Ar", "FLT-AIR-001", 35m, 280, 25),
            ("Filtro de Cabine", "FLT-CAB-001", 40m, 220, 20),
            ("Pastilhas de Freio Dianteiras", "BRK-PAD-F", 180m, 200, 20),
            ("Pastilhas de Freio Traseiras", "BRK-PAD-R", 160m, 180, 18),
            ("Disco de Freio Dianteiro", "BRK-DSC-F", 320m, 80, 10),
            ("Fluido de Freio DOT4", "BRK-FLD-4", 28m, 150, 15),
            ("Jogo de Velas", "IGN-SPK-4", 95m, 120, 12),
            ("Bobina de Ignição", "IGN-COIL-1", 145m, 60, 8),
            ("Bateria 60Ah", "ELC-BAT-60", 420m, 45, 8),
            ("Alternador", "ELC-ALT-1", 580m, 25, 5),
            ("Motor de Partida", "ELC-STR-1", 650m, 20, 4),
            ("Radiador", "CLG-RAD-1", 380m, 30, 5),
            ("Bomba d'Água", "CLG-PMP-1", 290m, 35, 6),
            ("Termostato", "CLG-THM-1", 75m, 50, 10),
            ("Kit Correia Dentada", "ENG-TBK-1", 340m, 40, 6),
            ("Correia Poly-V", "ENG-SBK-1", 85m, 70, 10),
            ("Kit de Embreagem", "DRV-CLK-1", 720m, 18, 4),
            ("Amortecedor Dianteiro", "SUS-SHK-F", 260m, 55, 8),
            ("Amortecedor Traseiro", "SUS-SHK-R", 240m, 50, 8),
            ("Ponta de Direção", "SUS-TRE-1", 120m, 65, 10),
            ("Pivô", "SUS-BAL-1", 110m, 60, 10),
            ("Pneu 185/65 R15", "TIR-185-65", 380m, 100, 12),
            ("Pneu 195/55 R16", "TIR-195-55", 420m, 90, 10),
            ("Rolamento de Roda", "WHL-BRG-1", 150m, 70, 10),
            ("Coifa de Homocinética", "DRV-CVB-1", 65m, 80, 12),
            ("Sonda Lambda", "EXH-O2S-1", 195m, 45, 8),
            ("Catalisador", "EXH-CAT-1", 890m, 12, 3),
            ("Silencioso", "EXH-MUF-1", 310m, 25, 5),
            ("Bomba de Combustível", "FUL-PMP-1", 340m, 22, 4),
            ("Filtro de Combustível", "FUL-FLT-1", 55m, 90, 12),
            ("Kit de Retentores dos Bicos", "FUL-ISK-1", 45m, 100, 15),
            ("Compressor do Ar-Condicionado", "AC-CMP-1", 950m, 10, 3),
            ("Condensador do Ar-Condicionado", "AC-CND-1", 480m, 15, 4),
            ("Palheta 22 pol.", "WIP-22-1", 35m, 120, 15),
            ("Lâmpada H7", "LGT-H7-1", 42m, 130, 15),
            ("Coxim do Motor", "ENG-MNT-1", 175m, 40, 6),
            ("Coxim do Câmbio", "TRN-MNT-1", 160m, 35, 6),
            ("Jogo de Juntas do Motor", "ENG-GSK-1", 220m, 30, 5),
            ("Retentor da Virabrequim", "ENG-SLK-1", 95m, 40, 8),
            ("Bomba da Direção Hidráulica", "STR-PSP-1", 410m, 18, 4),
            ("Mangueira da Direção Hidráulica", "STR-PSH-1", 130m, 35, 6),
            ("Sensor ABS", "BRK-ABS-1", 165m, 40, 6),
            ("Sensor de Velocidade da Roda", "WHL-SPD-1", 140m, 45, 8),
            ("Maçaneta da Porta", "BDY-HND-1", 85m, 30, 5),
            ("Retrovisor Lateral", "BDY-MIR-1", 210m, 20, 4),
            ("Kit de Clips do Para-choque", "BDY-CLP-1", 25m, 200, 25),
            ("Água Limpa Para-brisa 5L", "MNT-WSH-5", 18m, 250, 30)
        };

        var parts = new List<Part>(definitions.Length);
        foreach (var (name, sku, price, stock, min) in definitions)
        {
            var part = Part.Create(name, sku, price, stock, min);
            if (stock >= 200)
                part.AdjustStock(25, "Reabastecimento inicial dos dados de demonstração");
            parts.Add(part);
        }

        return parts;
    }

    private static List<Customer> CreateCustomers(int customerCount)
    {
        var customers = new List<Customer>(customerCount);

        var joao = Customer.Create("39053344705", "João Silva", "joao@example.com", "11999998888");
        joao.AddVehicle("ABC1D23", "Toyota", "Corolla", 2020);
        joao.AddVehicle("BCD2E34", "Honda", "Fit", 2018);
        customers.Add(joao);

        var maria = Customer.Create("11144477735", "Maria Santos", "maria@example.com", "11988887777");
        maria.AddVehicle("GHI3J45", "Fiat", "Uno", 2019);
        customers.Add(maria);

        for (var i = 2; i < customerCount; i++)
        {
            var firstName = FirstNames.Pick(i);
            var lastName = LastNames.Pick(i / FirstNames.Length);
            var name = $"{firstName} {lastName}";
            var document = SeedDataGenerator.GenerateCpf(10_000 + i);
            var email = $"cliente{i:000}@example.com";
            var phone = $"11{900000000 + i}";

            var customer = Customer.Create(document, name, email, phone);

            var vehicleCount = i % 5 == 0 ? 2 : 1;
            for (var v = 0; v < vehicleCount; v++)
            {
                var catalog = VehicleCatalog.Pick(i + v);
                var year = 2012 + (i + v) % 13;
                var plate = SeedDataGenerator.GenerateMercosulPlate(i * 3 + v + 100);
                customer.AddVehicle(plate, catalog.Brand, catalog.Model, year);
            }

            customers.Add(customer);
        }

        return customers;
    }

    private static WorkOrder CreateWorkOrder(
        int index,
        IReadOnlyList<Customer> customers,
        IReadOnlyList<Service> services,
        IReadOnlyList<Part> parts,
        IReadOnlyDictionary<Guid, Part> partsById)
    {
        var customer = index == 0
            ? customers.First(c => c.Document == "39053344705")
            : customers.Pick(index + 7);

        var vehicle = customer.Vehicles.Pick(index);
        var orderNumber = $"WO-2026-{index + 1:D5}";

        var serviceCount = 1 + index % 3;
        var serviceSelections = Enumerable.Range(0, serviceCount)
            .Select(offset => (services.Pick(index + offset), 1 + (index + offset) % 2))
            .ToList();

        var partSelections = index % 4 == 0
            ? []
            : new List<(Part Part, int Quantity)>
            {
                (parts.Pick(index), 1 + index % 2)
            };

        var notes = index % 7 == 0
            ? null
            : $"Ordem de demonstração #{index + 1} — gerada automaticamente para demonstração";

        var workOrder = WorkOrder.Create(
            orderNumber,
            customer.Id,
            vehicle.Id,
            serviceSelections,
            partSelections,
            notes);

        ApplyTargetStatus(workOrder, (WorkOrderStatus)(index % 6), partsById);
        return workOrder;
    }

    private static void ApplyTargetStatus(
        WorkOrder workOrder,
        WorkOrderStatus targetStatus,
        IReadOnlyDictionary<Guid, Part> partsById)
    {
        if (targetStatus >= WorkOrderStatus.InDiagnosis)
            workOrder.StartDiagnosis("Sistema");

        if (targetStatus >= WorkOrderStatus.AwaitingApproval)
            workOrder.SendBudgetForApproval("Sistema");

        if (targetStatus >= WorkOrderStatus.InExecution)
            workOrder.ApproveBudget(partsById, "Cliente", deductStock: false);

        if (targetStatus >= WorkOrderStatus.Completed)
            workOrder.Complete("Sistema");

        if (targetStatus >= WorkOrderStatus.Delivered)
            workOrder.Deliver("Sistema");
    }
}
