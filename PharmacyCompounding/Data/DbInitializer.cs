using PharmacyCompounding.Models;

namespace PharmacyCompounding.Data
{
    // Seeds the database with test data
    public static class DbInitializer
    {
        public static void Seed(PharmacyContext db)
        {
            HashPlainPasswords(db);
            EnsureManager(db);
            if (db.Warehouses.Any()) return;

            var today = DateTime.Today;
            var wh = new Warehouse { Name = "Основний склад", Address = "м. Київ, вул. Володимирська, 60" };
            var cold = new Warehouse { Name = "Холодильна камера", Address = "м. Київ, вул. Володимирська, 60, прим. 2" };

            var ing = new[]
            {
                new Ingredient { Code = "ING-001", Name = "Вазелін", Unit = "г", PricePerUnit = 0.45m, Stock = 2500, MinStock = 500, ExpiryDate = today.AddYears(2), Warehouse = wh },
                new Ingredient { Code = "ING-002", Name = "Ланолін безводний", Unit = "г", PricePerUnit = 1.20m, Stock = 800, MinStock = 200, ExpiryDate = today.AddYears(1), Warehouse = wh },
                new Ingredient { Code = "ING-003", Name = "Цинку оксид", Unit = "г", PricePerUnit = 0.90m, Stock = 600, MinStock = 100, ExpiryDate = today.AddYears(3), Warehouse = wh },
                new Ingredient { Code = "ING-004", Name = "Кислота борна", Unit = "г", PricePerUnit = 0.70m, Stock = 90, MinStock = 100, ExpiryDate = today.AddYears(2), Warehouse = wh },
                new Ingredient { Code = "ING-005", Name = "Вода очищена", Unit = "мл", PricePerUnit = 0.02m, Stock = 20000, MinStock = 5000, ExpiryDate = today.AddMonths(6), Warehouse = wh },
                new Ingredient { Code = "ING-006", Name = "Натрію хлорид", Unit = "г", PricePerUnit = 0.15m, Stock = 1500, MinStock = 300, ExpiryDate = today.AddYears(4), Warehouse = wh },
                new Ingredient { Code = "ING-007", Name = "Глюкоза", Unit = "г", PricePerUnit = 0.35m, Stock = 1200, MinStock = 300, ExpiryDate = today.AddYears(2), Warehouse = wh },
                new Ingredient { Code = "ING-008", Name = "Ментол", Unit = "г", PricePerUnit = 6.50m, Stock = 40, MinStock = 50, ExpiryDate = today.AddYears(1), Warehouse = wh },
                new Ingredient { Code = "ING-009", Name = "Анестезин", Unit = "г", PricePerUnit = 4.80m, Stock = 150, MinStock = 30, ExpiryDate = today.AddYears(2), Warehouse = wh },
                new Ingredient { Code = "ING-010", Name = "Новокаїн", Unit = "г", PricePerUnit = 5.20m, Stock = 120, MinStock = 30, ExpiryDate = today.AddYears(2), Warehouse = wh },
                new Ingredient { Code = "ING-011", Name = "Масло персикове", Unit = "мл", PricePerUnit = 1.10m, Stock = 400, MinStock = 100, ExpiryDate = today.AddMonths(8), Warehouse = cold },
                new Ingredient { Code = "ING-012", Name = "Протаргол", Unit = "г", PricePerUnit = 9.40m, Stock = 25, MinStock = 10, ExpiryDate = today.AddDays(-10), Warehouse = cold },
                new Ingredient { Code = "ING-013", Name = "Іхтіол", Unit = "г", PricePerUnit = 2.30m, Stock = 15, MinStock = 50, ExpiryDate = today.AddYears(1), Warehouse = cold },
            };

            var doctors = new[]
            {
                new Doctor { FullName = "Коваленко Ірина Петрівна", Specialization = "Дерматолог", Institution = "КНП «Київський міський шкірно-венерологічний диспансер»", LicenseNumber = "ЛК-104523" },
                new Doctor { FullName = "Мельник Олег Васильович", Specialization = "Педіатр", Institution = "КНП «Дитяча клінічна лікарня №2»", LicenseNumber = "ЛК-208817" },
                new Doctor { FullName = "Шевчук Наталія Андріївна", Specialization = "Терапевт", Institution = "КНП «ЦПМСД №1 Шевченківського району»", LicenseNumber = "ЛК-310942" },
                new Doctor { FullName = "Бойко Сергій Миколайович", Specialization = "Отоларинголог", Institution = "КНП «Київська міська клінічна лікарня №9»", LicenseNumber = "ЛК-412087" },
                new Doctor { FullName = "Олійник Галина Степанівна", Specialization = "Дерматолог", Institution = "ТОВ «Медичний центр «Добробут»", LicenseNumber = "ЛК-415530" },
                new Doctor { FullName = "Поліщук Андрій Вікторович", Specialization = "Хірург", Institution = "КНП «Київська міська клінічна лікарня №17»", LicenseNumber = "ЛК-503316" },
                new Doctor { FullName = "Руденко Оксана Іванівна", Specialization = "Педіатр", Institution = "КНП «ЦПМСД №2 Печерського району»", LicenseNumber = "ЛК-509921" },
                new Doctor { FullName = "Гаврилюк Петро Олегович", Specialization = "Офтальмолог", Institution = "КНП «Київська міська офтальмологічна лікарня «Центр мікрохірургії ока»", LicenseNumber = "ЛК-611204" },
                new Doctor { FullName = "Кузьменко Лариса Петрівна", Specialization = "Терапевт", Institution = "КНП «ЦПМСД Оболонського району»", LicenseNumber = "ЛК-618745" },
                new Doctor { FullName = "Марченко Юрій Анатолійович", Specialization = "Алерголог", Institution = "ДУ «Інститут отоларингології ім. О. С. Коломійченка НАМН України»", LicenseNumber = "ЛК-720058" },
            };

            var clients = new[]
            {
                new Client { FullName = "Бондаренко Андрій Сергійович", Phone = "+380671234567", Email = "bondarenko@example.com", Password = "client1", Address = "м. Київ, вул. Хрещатик, 15", BirthDate = new DateTime(1985, 4, 12) },
                new Client { FullName = "Ткаченко Марія Олексіївна", Phone = "+380502345678", Email = "tkachenko@example.com", Password = "client2", Address = "м. Київ, просп. Перемоги, 32", BirthDate = new DateTime(1992, 9, 3) },
                new Client { FullName = "Кравчук Дмитро Іванович", Phone = "+380933456789", Email = "kravchuk@example.com", Password = "client3", Address = "м. Київ, вул. Січових Стрільців, 7", BirthDate = new DateTime(1978, 1, 25) },
                new Client { FullName = "Лисенко Ольга Віталіївна", Phone = "+380674567890", Email = "lysenko@example.com", Password = "client4", Address = "м. Київ, вул. Антоновича, 44", BirthDate = new DateTime(1990, 6, 18) },
                new Client { FullName = "Мороз Ігор Леонідович", Phone = "+380505678901", Email = "moroz@example.com", Password = "client5", Address = "м. Київ, просп. Науки, 12", BirthDate = new DateTime(1969, 11, 2) },
                new Client { FullName = "Сидоренко Анна Юріївна", Phone = "+380936789012", Email = "sydorenko@example.com", Password = "client6", Address = "м. Київ, вул. Велика Васильківська, 100", BirthDate = new DateTime(2001, 2, 14) },
                new Client { FullName = "Павленко Максим Олегович", Phone = "+380677890123", Email = "pavlenko@example.com", Password = "client7", Address = "м. Київ, вул. Оболонська, 21", BirthDate = new DateTime(1995, 8, 30) },
                new Client { FullName = "Іваненко Світлана Петрівна", Phone = "+380508901234", Email = "ivanenko@example.com", Password = "client8", Address = "м. Київ, бульв. Лесі Українки, 5", BirthDate = new DateTime(1957, 3, 9) },
                new Client { FullName = "Гнатюк Роман Васильович", Phone = "+380939012345", Email = "hnatiuk@example.com", Password = "client9", Address = "м. Київ, вул. Гетьмана, 6", BirthDate = new DateTime(1983, 12, 1) },
                new Client { FullName = "Зінченко Вікторія Андріївна", Phone = "+380670123456", Email = "zinchenko@example.com", Password = "client10", Address = "м. Київ, вул. Митрополита Шептицького, 4", BirthDate = new DateTime(1998, 7, 22) },
            };

            var pharmacist = new Pharmacist { FullName = "Литвиненко Олена Миколаївна", Phone = "+380441112233", Email = "lytvynenko@pharmacy.ua", Password = "pharm", PersonnelNumber = "П-001", Position = "Провізор", HireDate = new DateTime(2018, 3, 1), QualificationCategory = "Вища" };
            var technologist = new PharmTechnologist { FullName = "Савченко Віктор Петрович", Phone = "+380442223344", Email = "savchenko@pharmacy.ua", Password = "tech", PersonnelNumber = "Т-001", Position = "Фармацевт-технолог", HireDate = new DateTime(2020, 9, 15), AdmittedToProduction = true };
            var pharmacist2 = new Pharmacist { FullName = "Козак Ірина Олександрівна", Phone = "+380443334455", Email = "kozak@pharmacy.ua", Password = "pharm2", PersonnelNumber = "П-002", Position = "Провізор", HireDate = new DateTime(2021, 5, 10), QualificationCategory = "Перша" };
            var technologist2 = new PharmTechnologist { FullName = "Демченко Артем Ігорович", Phone = "+380444445566", Email = "demchenko@pharmacy.ua", Password = "tech2", PersonnelNumber = "Т-002", Position = "Фармацевт-технолог", HireDate = new DateTime(2023, 1, 16), AdmittedToProduction = true };

            db.AddRange(ing);
            db.AddRange(doctors);
            db.AddRange(clients);
            db.Add(pharmacist);
            db.Add(technologist);
            db.Add(pharmacist2);
            db.Add(technologist2);

            Prescription Rx(string num, Doctor d, int daysAgo, string patient, string formula) =>
                new() { Number = num, Doctor = d, IssueDate = today.AddDays(-daysAgo), ValidityDays = 30, Patient = patient, Formula = formula };

            OrderItem Item(string name, string form, string dosage, decimal work, params (Ingredient i, double a)[] comps) =>
                new()
                {
                    MedicineName = name, DosageForm = form, Dosage = dosage, Quantity = 1, WorkCost = work,
                    Components = comps.Select(c => new Component { Ingredient = c.i, Amount = c.a, Unit = c.i.Unit }).ToList()
                };

            var orders = new List<Order>
            {
                new() { Number = 1001, CreatedAt = today.AddDays(-12).AddHours(10), PlannedReadyDate = today.AddDays(-9), Status = OrderStatus.Issued,
                        Client = clients[0], Prescription = Rx("RX-2026-0411", doctors[0], 13, "Бондаренко А. С.", "Rp.: Zinci oxydi 5,0; Vaselini ad 50,0. M.f. ungt."),
                        Items = { Item("Цинкова мазь 10%", "Мазь", "50 г", 85m, (ing[2], 5), (ing[0], 45)) } },
                new() { Number = 1002, CreatedAt = today.AddDays(-4).AddHours(11), PlannedReadyDate = today.AddDays(-1), Status = OrderStatus.Ready,
                        Client = clients[1], Prescription = Rx("RX-2026-0503", doctors[1], 5, "Ткаченко С. А. (дитина)", "Rp.: Sol. Natrii chloridi 0,9% — 200 ml"),
                        Items = { Item("Розчин натрію хлориду 0,9%", "Розчин", "200 мл", 60m, (ing[5], 1.8), (ing[4], 200)) } },
                new() { Number = 1003, CreatedAt = today.AddDays(-2).AddHours(9), PlannedReadyDate = today.AddDays(1), Status = OrderStatus.InProduction,
                        Client = clients[2], Prescription = Rx("RX-2026-0517", doctors[2], 3, "Кравчук Д. І.", "Rp.: Mentholi 1,0; Anaesthesini 2,0; Vaselini 30,0"),
                        Items = { Item("Мазь з ментолом та анестезином", "Мазь", "33 г", 95m, (ing[7], 1), (ing[8], 2), (ing[0], 30)) } },
                new() { Number = 1004, CreatedAt = today.AddDays(-1).AddHours(15), PlannedReadyDate = today.AddDays(2), Status = OrderStatus.AwaitingPayment,
                        Client = clients[0], Prescription = Rx("RX-2026-0522", doctors[0], 2, "Бондаренко А. С.", "Rp.: Lanolini anhydrici 20,0; Vaselini 30,0"),
                        Items = { Item("Основа мазева ланолін-вазелінова", "Мазь", "50 г", 70m, (ing[1], 20), (ing[0], 30)) } },
                new() { Number = 1005, CreatedAt = today.AddHours(9).AddMinutes(30), PlannedReadyDate = today.AddDays(3), Status = OrderStatus.UnderReview,
                        Client = clients[1], Prescription = Rx("RX-2026-0530", doctors[2], 0, "Ткаченко М. О.", "Rp.: Sol. Glucosi 5% — 100 ml"),
                        Items = { Item("Розчин глюкози 5%", "Розчин", "100 мл", 55m, (ing[6], 5), (ing[4], 100)) } },
                new() { Number = 1006, CreatedAt = today.AddDays(-6).AddHours(14), PlannedReadyDate = today.AddDays(-3), Status = OrderStatus.Rejected,
                        Client = clients[2], Prescription = Rx("RX-2026-0302", doctors[1], 45, "Кравчук О. Д.", "Rp.: Novocaini 0,5; Aquae purificatae 100 ml"),
                        Items = { Item("Розчин новокаїну 0,5%", "Розчин", "100 мл", 50m, (ing[9], 0.5), (ing[4], 100)) } },
            };
            foreach (var o in orders) o.CalculateTotal();
            db.AddRange(orders);

            // History for the processed and ready orders
            foreach (var o in orders.Where(o => o.Status is OrderStatus.Issued or OrderStatus.Ready))
            {
                o.Payment = new Payment { Amount = o.Total, Date = o.CreatedAt.AddHours(2), Method = "Картка", Successful = true };
                var act = new ManufacturingAct
                {
                    Number = o.Number, Order = o, Technologist = technologist,
                    ManufacturedAt = o.CreatedAt.AddDays(1), Batch = $"S-{o.Number}-1", Notes = "Виготовлено за прописом"
                };
                act.QualityCheck = new QualityCheck
                {
                    Pharmacist = pharmacist, Date = act.ManufacturedAt.AddHours(2),
                    Organoleptic = true, Physical = true, Chemical = true, Result = true
                };
                db.Add(act);
            }
            // Order in production: paid, act created, awaiting quality control
            var inProduction = orders.First(o => o.Status == OrderStatus.InProduction);
            inProduction.Payment = new Payment { Amount = inProduction.Total, Date = inProduction.CreatedAt.AddHours(1), Method = "Готівка", Successful = true };
            db.Add(new ManufacturingAct
            {
                Number = inProduction.Number, Order = inProduction, Technologist = technologist,
                ManufacturedAt = today.AddHours(8), Batch = $"S-{inProduction.Number}-1", Notes = "Виготовлено за прописом"
            });

            // Last six months of history: picked-up and cancelled orders with payments, acts and quality checks
            var history = new (int Number, int DaysAgo, Client Client, Doctor Doctor, OrderItem Item, bool Cancelled, PharmTechnologist Tech, Pharmacist Qc, bool Defect)[]
            {
                (901, 170, clients[3], doctors[0], Item("Цинкова мазь 10%", "Мазь", "50 г", 85m, (ing[2], 5), (ing[0], 45)), false, technologist, pharmacist, false),
                (902, 158, clients[4], doctors[3], Item("Краплі ментолові в олії 1%", "Краплі", "20 мл", 65m, (ing[7], 0.2), (ing[10], 20)), false, technologist, pharmacist, false),
                (903, 141, clients[0], doctors[4], Item("Мазь іхтіолова 10%", "Мазь", "30 г", 80m, (ing[12], 3), (ing[0], 27)), false, technologist2, pharmacist2, true),
                (904, 127, clients[5], doctors[6], Item("Розчин натрію хлориду 0,9%", "Розчин", "200 мл", 60m, (ing[5], 1.8), (ing[4], 200)), false, technologist, pharmacist, false),
                (905, 112, clients[6], doctors[2], Item("Розчин глюкози 5%", "Розчин", "100 мл", 55m, (ing[6], 5), (ing[4], 100)), true, technologist, pharmacist, false),
                (906, 96, clients[7], doctors[5], Item("Мазь з анестезином 5%", "Мазь", "40 г", 90m, (ing[8], 2), (ing[0], 38)), false, technologist2, pharmacist, false),
                (907, 83, clients[1], doctors[7], Item("Краплі протарголу 2%", "Краплі", "10 мл", 75m, (ing[11], 0.2), (ing[4], 10)), false, technologist, pharmacist2, false),
                (908, 70, clients[8], doctors[8], Item("Розчин борної кислоти 2%", "Розчин", "100 мл", 50m, (ing[3], 2), (ing[4], 100)), false, technologist2, pharmacist2, false),
                (909, 58, clients[2], doctors[0], Item("Паста цинкова", "Паста", "50 г", 95m, (ing[2], 12.5), (ing[0], 37.5)), false, technologist, pharmacist, true),
                (910, 44, clients[9], doctors[1], Item("Розчин новокаїну 0,5%", "Розчин", "100 мл", 50m, (ing[9], 0.5), (ing[4], 100)), false, technologist2, pharmacist, false),
                (911, 33, clients[3], doctors[3], Item("Краплі ментолові в олії 1%", "Краплі", "20 мл", 65m, (ing[7], 0.2), (ing[10], 20)), true, technologist, pharmacist, false),
                (912, 25, clients[4], doctors[4], Item("Основа мазева ланолін-вазелінова", "Мазь", "50 г", 70m, (ing[1], 20), (ing[0], 30)), false, technologist, pharmacist2, false),
                (913, 19, clients[5], doctors[9], Item("Мазь з ментолом та анестезином", "Мазь", "33 г", 95m, (ing[7], 1), (ing[8], 2), (ing[0], 30)), false, technologist2, pharmacist, false),
                (914, 15, clients[6], doctors[5], Item("Цинкова мазь 10%", "Мазь", "50 г", 85m, (ing[2], 5), (ing[0], 45)), false, technologist, pharmacist2, false),
            };
            foreach (var h in history)
            {
                var created = today.AddDays(-h.DaysAgo).AddHours(9 + h.Number % 8);
                var o = new Order
                {
                    Number = h.Number, CreatedAt = created, PlannedReadyDate = created.Date.AddDays(3),
                    Status = h.Cancelled ? OrderStatus.Cancelled : OrderStatus.Issued, Client = h.Client,
                    Prescription = Rx($"RX-2026-{h.Number - 800:0000}", h.Doctor, h.DaysAgo + 1, h.Client.FullName, h.Item.MedicineName),
                    Items = { h.Item }
                };
                o.CalculateTotal();
                db.Add(o);
                if (h.Cancelled) continue;   // cancelled before payment

                o.Payment = new Payment { Amount = o.Total, Date = created.AddHours(1), Method = h.Number % 3 == 0 ? "Готівка" : "Картка", Successful = true };
                // The first batch may fail quality control, in which case it is made again
                for (var attempt = 1; attempt <= (h.Defect ? 2 : 1); attempt++)
                {
                    var passed = !h.Defect || attempt == 2;
                    var act = new ManufacturingAct
                    {
                        Number = h.Number * 10 + attempt, Order = o, Technologist = h.Tech,
                        ManufacturedAt = created.AddDays(attempt), Batch = $"S-{h.Number}-{attempt}",
                        Notes = passed ? "Виготовлено за прописом" : "Брак: неоднорідна консистенція"
                    };
                    act.QualityCheck = new QualityCheck
                    {
                        Pharmacist = h.Qc, Date = act.ManufacturedAt.AddHours(2),
                        Organoleptic = passed, Physical = true, Chemical = true, Result = passed
                    };
                    db.Add(act);
                }
            }

            foreach (var u in clients.Cast<User>().Append(pharmacist).Append(technologist).Append(pharmacist2).Append(technologist2))
                u.SetPassword(u.Password!);

            db.SaveChanges();
        }

        // Employees don't sign up themselves, the manager adds them, so the system always needs at least one manager
        private static void EnsureManager(PharmacyContext db)
        {
            if (db.Managers.Any()) return;
            var manager = new PharmacyManager
            {
                FullName = "Гончаренко Тетяна Василівна", Phone = "+380440001122", Email = "manager@pharmacy.ua",
                PersonnelNumber = "З-001", Position = "Завідувач аптеки", HireDate = new DateTime(2015, 2, 2)
            };
            manager.SetPassword("manager");
            db.Add(manager);
            db.SaveChanges();
        }

        // Databases created before sign-in existed store plain-text passwords, so hash them
        private static void HashPlainPasswords(PharmacyContext db)
        {
            var plain = db.Users.Where(u => u.Password != null && !u.Password.StartsWith("AQAAAA")).ToList();
            foreach (var u in plain) u.SetPassword(u.Password!);
            if (plain.Count > 0) db.SaveChanges();
        }
    }
}
