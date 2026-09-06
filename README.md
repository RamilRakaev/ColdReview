# Cold Review

Visual Studio debugger extension that shows the full method call chain (OzCode Reveal style).

Stopped inside `OrderRepository.Create`? Cold Review shows:

`Program.Main → OrdersController.Create → CreateOrderHandler.Handle → OrderRepository.Create`

Each frame lists arguments, file, and line. Click a card to switch the current stack frame and jump to source.

---

## English

### Install

Double-click `dist/ColdReview.vsix`, or in Visual Studio: **Extensions → Install from VSIX…**. Restart Visual Studio when prompted.

### Open the window

**Tools → Cold Review**

Also available from:

- **Debug → Cold Review: вложенность вызовов**
- **View → Other Windows → Cold Review**
- `Ctrl+Shift+Alt+C`

The window opens automatically on the first breakpoint.

### Use it

1. Set a breakpoint in a nested method (for example `OrderRepository.Create`).
2. Start debugging (**F5**).
3. Open **Tools → Cold Review** if the window is closed.
4. Read the chain left to right: caller → callee. The current method is marked **вы здесь**.
5. Click a frame to navigate. **Copy** puts the path on the clipboard. Uncheck **Только пользовательский код** to include `System.*` frames.

A compact path also appears at the top of the editor while you are in break mode.

---

## Русский

### Установка

Дважды щёлкните `dist/ColdReview.vsix` или в Visual Studio: **Extensions → Install from VSIX…**. После установки перезапустите Visual Studio.

### Как открыть окно

**Tools → Cold Review**

Также:

- **Debug → Cold Review: вложенность вызовов**
- **View → Other Windows → Cold Review**
- `Ctrl+Shift+Alt+C`

На первом breakpoint окно открывается само.

### Как пользоваться

1. Поставьте breakpoint во вложенном методе (например `OrderRepository.Create`).
2. Запустите отладку (**F5**).
3. Если окно закрыли — **Tools → Cold Review**.
4. Цепочка читается слева направо: вызывающий → вызываемый. Текущий метод помечен **вы здесь**.
5. Клик по карточке переключает кадр стека. **Копировать** сохраняет цепочку в буфер. Снимите **Только пользовательский код**, чтобы увидеть кадры `System.*`.

Пока отладчик на паузе, краткая цепочка также показывается сверху в редакторе.
