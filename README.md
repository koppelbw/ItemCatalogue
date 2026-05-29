# ItemCatalogue
Purpose: Track items you own — where they're stored, who owns them, their type and price, and whether they've been disposed of and why. 




# Architecture
A clean architecture ASP.NET Core 10 Web API for cataloging physical items in a home or space.
 Hexagonal (ports & adapters)




# Notes
Yes and no — it's valid, but the split is more natural on the repository side than the service side.

Why IItemRepository (Domain) / ItemRepository (Persistence) is a clean ports-and-adapters split:
The repository port is a driven/secondary port — it's how your application core reaches out to infrastructure. The interface belongs to the core, the implementation belongs to the infrastructure layer. That separation has real value: you can swap SQL Server for something else without touching the domain.

Why IItemService (Application) / ItemService (Service) is a weaker split:
ItemService isn't really an "adapter" — it's not wrapping external infrastructure or translating between systems. It is your application logic. In most clean architecture implementations, the service interface and its implementation both live in the Application project together. The Service project ends up being a thin pass-through layer with no real architectural justification.


The common convention:

Layer	
    Contains
Domain	
    Entities, enums, IItemRepository
Application
    IItemService + ItemService, use case logic
Persistence	
    ItemRepository, DbContext
API	
    Controllers, DI wiring

The main reason to keep them split is if you want Application to be a pure contracts assembly — but that's uncommon and adds project complexity without much payoff for a project this size.

Short answer: Move ItemService into the Application project alongside IItemService, and delete the Service project. Save the ports/adapters split for the repository layer where it genuinely matters.





What is the point of IItemService if the interface and service live in the same project?

Good question. Honestly, if they're in the same project, the interface buys you very little in practice.

The real reasons to keep IItemService:

Unit testing — you can mock IItemService in controller tests without spinning up the real service and its dependencies
Multiple implementations — if you ever genuinely needed a second implementation (e.g. a cached version, a read-only variant), the interface is already there
But counterarguments:

Controllers only ever get one real implementation injected — the interface is just indirection for its own sake
For testing, you can mock a concrete class too (with virtual methods), or better yet, test the service directly rather than mocking it out from the controller
"We might need a second implementation someday" is speculative
The honest take: IItemService at the application layer is largely a habit carried over from pre-DI-container .NET days, when interfaces were needed for testability tooling. Modern projects often skip it entirely and just register the concrete ItemService directly with the DI container.

The interface genuinely earns its place on the repository side (IItemRepository) because you actually might swap persistence — in-memory for tests, different DB provider, etc. On the service side it's usually ceremony.