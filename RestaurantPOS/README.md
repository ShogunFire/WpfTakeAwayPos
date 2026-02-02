# Restaurant Point of Sale (POS) System

This project is a WPF application designed for managing restaurant operations, specifically focusing on order entry, kitchen display, and payment processing. It follows the MVVM (Model-View-ViewModel) architectural pattern to separate concerns and enhance maintainability.

## Features

- **Order Entry**: Allows staff to create and manage customer orders, select menu items, and submit orders.
- **Kitchen Display**: Displays current orders for kitchen staff, enabling them to track and manage order preparation.
- **Payment Processing**: Facilitates payment transactions, allowing staff to process payments and manage payment methods.

## Project Structure

- **Models**: Contains classes that represent the core data structures of the application.
  - `Order.cs`: Defines the Order class with properties and methods related to customer orders.
  - `MenuItem.cs`: Defines the MenuItem class with properties and methods for menu items.
  - `Table.cs`: Defines the Table class for managing table states.
  - `Payment.cs`: Defines the Payment class for processing payments.

- **ViewModels**: Contains classes that handle the logic and state of the views.
  - `OrderEntryViewModel.cs`: Manages the order entry process.
  - `KitchenDisplayViewModel.cs`: Manages the kitchen display of current orders.
  - `PaymentViewModel.cs`: Manages payment processing.
  - `BaseViewModel.cs`: Provides a base class for view models with property change notification support.

- **Views**: Contains XAML files that define the user interface.
  - `OrderEntryView.xaml`: Layout for the order entry screen.
  - `KitchenDisplayView.xaml`: Layout for the kitchen display screen.
  - `PaymentView.xaml`: Layout for the payment processing screen.

## Setup Instructions

1. Clone the repository to your local machine.
2. Open the solution in your preferred IDE.
3. Restore the NuGet packages if necessary.
4. Build the solution.
5. Run the application.

## Contributing

Contributions are welcome! Please feel free to submit a pull request or open an issue for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.