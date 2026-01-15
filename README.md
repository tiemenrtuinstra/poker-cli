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

## 🔑 AI Setup (Optional)

The game works out of the box with **basic AI opponents**. To enable **LLM-powered AI** (smarter, more realistic opponents), you need API keys from one or more providers.

### Step 1: Get API Keys (free tiers available)

| Provider | Get API Key | Free Tier |
|----------|-------------|-----------|
| **Claude** (Anthropic) | [console.anthropic.com](https://console.anthropic.com/) | $5 free credit |
| **Gemini** (Google) | [aistudio.google.com/apikey](https://aistudio.google.com/apikey) | Free |
| **OpenAI** | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) | Pay-as-you-go |

> 💡 **Tip:** You only need ONE provider. Gemini is free and works great!

### Step 2: Create Environment File

Create a `.env` file in the same folder as `poker-cli`:

**Linux / macOS:**
```bash
cd ~/.local/bin
nano .env
```

**Windows:**
```powershell
cd $env:LOCALAPPDATA\Programs\poker-cli
notepad .env
```

### Step 3: Add Your API Keys

```env
# Add the keys you have (you don't need all three)
CLAUDE_API_KEY=sk-ant-xxxxxxxxxxxxx
GEMINI_API_KEY=AIzaxxxxxxxxxxxxx
OPENAI_API_KEY=sk-xxxxxxxxxxxxx

# Optional: Override default models
# CLAUDE_MODEL=claude-sonnet-4-20250514
# GEMINI_MODEL=gemini-2.0-flash
# OPENAI_MODEL=gpt-4o-mini
```

### Step 4: Play!

```bash
poker-cli
```

The game automatically detects which API keys are available and uses those providers for AI players. Without any API keys, the game uses basic rule-based AI.

---

## ✨ Features

### 🎮 Game Features
- **Complete Texas Hold'em Implementation**: Full rules with pre-flop, flop, turn, and river rounds
- **Multi-Player Support**: 1-8 players (human + AI), hot-seat multiplayer
- **🌐 LAN Multiplayer**: Host or join games over your local network with chat
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

## 🌐 LAN Multiplayer

Play poker with friends over your local network! The LAN multiplayer feature supports:
- **Host or Join**: Create a lobby or join an existing one via lobby code
- **Mixed Players**: Play with human players AND AI opponents
- **Real-time Chat**: Chat with other players during the game
- **AI Chat**: AI players participate in chat with personality-driven responses
- **Reconnection**: Automatically reconnect if your connection drops
- **Bot Takeover**: AI takes over for disconnected players

### Hosting a Game

1. Select **🌐 Multiplayer** from the main menu
2. Choose **Host Game**
3. Configure your lobby settings:
   - Lobby name and max players
   - Starting chips and blinds
   - Number of AI players
4. Share the **lobby code** (e.g., `ABC123`) with friends
5. Wait for players to join and ready up
6. Start the game when everyone is ready

### Joining a Game

1. Select **🌐 Multiplayer** from the main menu
2. Choose **Join Game**
3. Enter the lobby code provided by the host
4. Wait in the lobby and mark yourself as ready
5. Game starts when the host initiates

### Chat Features

- **Player Chat**: Send messages to all players with Enter
- **AI Commentary**: AI players comment on game events
- **AI Responses**: AI players may respond to your messages
- **System Messages**: Join/leave notifications
- **Rate Limiting**: 5 messages per 10 seconds to prevent spam

### Technical Details

| Setting | Value |
|---------|-------|
| Default Port | 7777 |
| Protocol | WebSocket |
| Reconnect Window | 5 minutes |
| Bot Takeover | After 30 seconds |
| Max Message Length | 500 characters |

### Network Troubleshooting

- **Can't connect?** Ensure firewall allows port 7777
- **Lost connection?** The game will auto-reconnect within 5 minutes
- **Player disconnected?** AI takes over after 30 seconds

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
├── Network/         # LAN Multiplayer system
│   ├── Server/      # WebSocket server & lobby management
│   ├── Client/      # WebSocket client & reconnection
│   ├── Messages/    # Network protocol messages
│   └── Chat/        # Chat system with AI participation
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

## 🛠️ Building & Releasing

### Local Development
```bash
# Build and run
dotnet build TexasHoldem.sln
dotnet run --project TexasHoldem
```

### Creating a Release Build
Build a self-contained executable with all dependencies (including Spectre.Console):

**Windows (PowerShell):**
```powershell
# Build for current platform (Windows x64)
.\build-release.ps1

# Build for specific platform
.\build-release.ps1 -Runtime linux-x64
.\build-release.ps1 -Runtime osx-arm64

# Build for all platforms
.\build-all-platforms.ps1
```

**Linux / macOS:**
```bash
# Make script executable
chmod +x build-release.sh

# Build for current platform
./build-release.sh linux-x64

# Build for other platforms
./build-release.sh osx-arm64
./build-release.sh win-x64
```

### Creating a GitHub Release
1. Go to GitHub → **Actions** tab
2. Select **"Build and Release"** workflow
3. Click **"Run workflow"**
4. Choose version bump: `patch` (1.2.7→1.2.8), `minor` (1.2.7→1.3.0), or `major` (1.2.7→2.0.0)
5. The workflow creates executables for all platforms and publishes them

### Supported Platforms
| Platform | Runtime ID |
|----------|------------|
| Windows x64 | `win-x64` |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |
| macOS Intel | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |

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
- **Tournament Brackets**: Multi-table tournament support
- **Advanced Statistics**: Heat maps, position analysis, tendency tracking
- **AI Training**: Machine learning for adaptive AI opponents
- **Custom UI Themes**: Personalized visual experiences
- **Spectator Mode**: Watch games without participating

### Extensibility Points
- **IPlayer Interface**: Add new player types (scripted, tournament bots, etc.)
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