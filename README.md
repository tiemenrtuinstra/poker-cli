# 🃏 Texas Hold'em Poker CLI

A feature-rich, extensible Texas Hold'em Poker command-line game built with .NET 10. Experience authentic poker gameplay with intelligent AI opponents (Claude, Gemini, OpenAI), comprehensive logging, and endless customization options.

## 📦 Quick Install

### Linux / macOS
```bash
curl -fsSL https://raw.githubusercontent.com/tiemenrtuinstra/poker-cli/main/install.sh | bash
```

### Windows (PowerShell)
```powershell
irm https://raw.githubusercontent.com/tiemenrtuinstra/poker-cli/main/install.ps1 | iex
```

After installation, run `poker-cli` to start playing!

## ✨ Features

### 🎮 Game Features
- **Complete Texas Hold'em Implementation**: Full rules with pre-flop, flop, turn, and river rounds
- **Multi-Player Support**: 1-8 players (human + AI), hot-seat multiplayer
- **Intelligent AI System**: 11 unique AI personalities with realistic decision-making
- **Tournament Mode**: Blind increases, elimination format, comprehensive statistics
- **Hand History & Replay**: Complete game logging with JSON export/import
- **Rich CLI Experience**: ASCII art tables, colored output, intuitive menus

### 🤖 AI Personalities
- **The Professional**: Tight, calculated play
- **The Gambler**: Loose, aggressive action  
- **The Rock**: Ultra-conservative strategy
- **The Maniac**: Wild, unpredictable moves
- **The Fish**: Beginner-friendly opponent
- **The Shark**: Advanced strategic play
- **The Bluffer**: Deception-focused gameplay
- **The Calculator**: Mathematics-based decisions
- **The Loose Cannon**: Explosive betting patterns
- **The Nit**: Extremely tight play
- **The Calling Station**: Passive, call-heavy style

### 🔧 Configuration & Customization
- **Flexible Setup**: Starting chips, blinds, ante, tournament settings
- **Persistent Configuration**: JSON-based settings management
- **Preset Game Modes**: Quick start with balanced configurations
- **Extensive Logging**: Action logs, hand histories, player statistics
- **Replay System**: Review and analyze past games

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows/Linux/macOS terminal with color support

### Installation & Setup

1. **Clone or Download the Project**
   ```bash
   git clone <repository-url>
   cd poker-cli
   ```

2. **Build the Solution**
   ```bash
   dotnet build TexasHoldem.sln
   ```

3. **Run the Game**
   ```bash
   cd TexasHoldem
   dotnet run
   ```

4. **Run Tests (Optional)**
   ```bash
   dotnet test
   ```

## 🎯 How to Play

### Main Menu Options
1. **Start New Game**: Configure a custom poker game
2. **Load Preset Configuration**: Choose from balanced presets
3. **Load From Configuration**: Use your saved settings
4. **Manage Settings**: Update default configurations
5. **View Rules**: Learn Texas Hold'em basics

### Game Flow
1. **Setup**: Choose players, chips, blinds
2. **Pre-Game**: AI personalities are randomly assigned  
3. **Hand Play**: Standard Texas Hold'em rounds (Pre-flop → Flop → Turn → River)
4. **Actions**: Check, Call, Raise, Fold with intelligent prompts
5. **Showdown**: Automatic hand evaluation and winner determination
6. **Statistics**: View comprehensive player and game stats

### Controls
- **Navigate Menus**: Use number keys to select options
- **Betting Actions**: Enter amounts or choose from suggested bets
- **Game Control**: Continue hands or quit to main menu at any time

## 🏗️ Project Architecture

### Core Structure
```
TexasHoldem/
├── Domain/          # Core game models and logic
│   ├── Card.cs      # Card representation with parsing
│   ├── Deck.cs      # Deck management and shuffling  
│   ├── Enums/       # Game enumerations
│   ├── GameState.cs # Game state tracking
│   └── HandEvaluator.cs # Poker hand ranking engine
├── Game/            # Game engine components
│   ├── BettingLogic.cs  # Betting round management
│   ├── Dealer.cs        # Card dealing logic
│   ├── Pot.cs          # Pot and side pot handling
│   ├── Round.cs        # Hand orchestration
│   └── TexasHoldemGame.cs # Main game controller
├── Players/         # Player implementations
│   ├── IPlayer.cs      # Player interface
│   ├── HumanPlayer.cs  # Human player logic
│   ├── AiPlayer.cs     # AI player base
│   └── AiPersonality.cs # AI strategy engine
├── CLI/             # User interface system  
│   ├── Menu.cs         # Main menu system
│   ├── GameUI.cs       # Game display logic
│   ├── InputHelper.cs  # Input validation
│   ├── GameConfig.cs   # Configuration model
│   ├── ConfigurationManager.cs # Settings management
│   └── Logger.cs       # Logging system
└── Program.cs       # Application entry point
```

### Key Design Patterns
- **Interface Segregation**: Clean player abstractions
- **Strategy Pattern**: AI personality system
- **State Management**: Comprehensive game state tracking
- **Observer Pattern**: Event-driven logging
- **Factory Pattern**: AI name and personality generation

## 🔧 Configuration

### Default Settings (`config.json`)
```json
{
  "game": {
    "defaultHumanPlayers": 1,
    "defaultAiPlayers": 5,
    "defaultStartingChips": 10000,
    "defaultSmallBlind": 50,
    "defaultBigBlind": 100,
    "useColors": true,
    "enableAsciiArt": true,
    "enableLogging": true
  },
  "tournament": {
    "enableBlindIncrease": false,
    "blindIncreaseInterval": 10,
    "blindIncreaseMultiplier": 1.5
  },
  "ai": {
    "thinkingDelayMin": 500,
    "thinkingDelayMax": 2000,
    "enablePokerTalk": true,
    "pokerTalkFrequency": 0.2
  }
}
```

### Customization Options
- **Player Count**: 1-8 total players (any mix of human/AI)
- **Starting Chips**: 1,000 - 1,000,000 per player
- **Blind Structure**: Configurable small/big blinds and ante
- **Tournament Settings**: Blind increases, elimination format
- **Display Options**: Colors, ASCII art, animation speed
- **AI Behavior**: Thinking delays, personality talk frequency

## 📊 Game Logs & Statistics

### Logging Features
- **Action Logs**: Every bet, call, fold, and raise
- **Hand History**: Complete hand breakdowns with hole cards
- **Game Statistics**: Win rates, average pot size, showdown frequency  
- **Player Analytics**: Individual player performance metrics
- **JSON Export**: Machine-readable game data

### Log Files Location
```
logs/
├── game_20241201_143022.json     # Complete game session
├── hands_20241201_143022.json    # Detailed hand history
└── actions_20241201_143022.json  # Action-by-action log
```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "HandEvaluatorTests"

# Run with detailed output
dotnet test --verbosity normal
```

### Test Coverage
- **Hand Evaluation**: All poker hands, edge cases, tie-breaking
- **Game Logic**: Betting rounds, pot calculation, player elimination
- **AI Behavior**: Decision trees, personality consistency
- **Configuration**: Settings validation, file I/O operations

## 🎮 Game Presets

### 1. Tournament Mode (8 Players)
- **Players**: 1 Human, 7 AI
- **Chips**: 10,000 each
- **Blinds**: 50/100 with 10 ante
- **Features**: Blind increases every 10 hands

### 2. Casual Play (6 Players)  
- **Players**: 1 Human, 5 AI
- **Chips**: 15,000 each
- **Blinds**: 25/50, no ante
- **Features**: Fixed blinds, relaxed pace

### 3. High Stakes (4 Players)
- **Players**: 1 Human, 3 AI  
- **Chips**: 50,000 each
- **Blinds**: 500/1000 with 100 ante
- **Features**: Aggressive AI personalities

### 4. Beginner Friendly (4 Players)
- **Players**: 1 Human, 3 AI
- **Chips**: 20,000 each  
- **Blinds**: 10/20, no ante
- **Features**: Conservative AI, slower pace

### 5. AI Showcase (8 AI Players)
- **Players**: 0 Human, 8 AI
- **Chips**: 10,000 each
- **Blinds**: 50/100 with ante
- **Features**: Watch AI personalities interact

## 🛠️ Advanced Features

### Hand Evaluator Engine
- **7-Card Optimization**: Finds best 5-card hand from 7 available
- **Complete Rankings**: All poker hands from High Card to Royal Flush
- **Tie Breaking**: Accurate kicker comparison and side pot distribution
- **Performance**: Optimized algorithms for real-time evaluation

### AI Decision Engine  
- **Personality-Driven**: Each AI has consistent behavioral patterns
- **Hand Strength Analysis**: Contextual decision making based on position
- **Pot Odds Calculation**: Mathematical betting decisions where appropriate
- **Bluffing Logic**: Strategic deception based on personality type

### Logging System
- **Event Sourcing**: Complete game reconstruction from logs
- **JSON Format**: Structured data for analysis tools
- **Performance Metrics**: Track player statistics over time
- **Replay Capability**: Review interesting hands and decisions

## 🔮 Future Enhancements

### Planned Features
- **Online Multiplayer**: Network-based multi-player games
- **Tournament Brackets**: Multi-table tournament support
- **Advanced Statistics**: Heat maps, position analysis, tendency tracking
- **AI Training**: Machine learning for adaptive AI opponents
- **Custom UI Themes**: Personalized visual experiences

### Extensibility Points
- **IPlayer Interface**: Add new player types (network, scripted, etc.)
- **Personality System**: Create custom AI behaviors
- **Game Variants**: Omaha, Seven-Card Stud support
- **Logging Providers**: Database, cloud storage integration

## 📝 Contributing

### Development Setup
1. Install .NET 8 SDK
2. Clone repository
3. Open in VS Code or Visual Studio
4. Install recommended extensions

### Code Style
- **C# Conventions**: Follow Microsoft coding standards
- **Naming**: PascalCase for classes, camelCase for fields
- **Documentation**: XML docs for public APIs
- **Testing**: Unit tests for core logic

### Pull Request Process
1. Create feature branch from main
2. Implement changes with tests
3. Update documentation as needed
4. Submit PR with descriptive title

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Acknowledgments

- **Texas Hold'em Rules**: Official poker hand rankings and gameplay
- **ASCII Art**: Custom table layouts and card representations  
- **AI Design**: Inspired by real poker player archetypes
- **.NET Community**: Extensive use of modern C# features

---

### 🎲 Ready to Play?

```bash
cd TexasHoldem
dotnet run
```

**Good luck at the tables! 🍀**