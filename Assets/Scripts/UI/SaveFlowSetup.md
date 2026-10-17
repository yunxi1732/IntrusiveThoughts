# 存档 UI 绑定

UI 布局由场景自行配置。这里不自动修改按钮 OnClick。

## 常驻选槽位控制器

在 MainScene 已有的常驻 Canvas（StayCanvas）下创建一个始终启用的 SaveUI 对象，挂 SaveSlot_UI。
不要把控制器挂在会关闭的选槽位面板、MainPanel 或 SettingsPanel 上。
选槽位面板与确认面板也要在这个常驻 Canvas 下，才能供普通游戏和 EndScene 共用。
这两个面板应是独立的全屏弹窗，遮挡并拦截后方点击；确认面板放在选槽位面板上方。

SaveSlot_UI 字段：

- Slot Panel：四槽位面板。
- Confirm Panel：保存/覆盖/读取确认面板，必须与槽位按钮区分开。
- Confirm Text：确认提示文字。
- Message Text：失败原因提示，可选但推荐配置。
- Slots：4 个元素，每个填写 Slot Id（1～4）、Button 和 Summary（TMP 文本）。
  Summary 显示 Day、地点、已通关；空槽显示空槽位，损坏槽显示不兼容提示。

按钮 OnClick：

| 按钮 | 对象/函数 |
| --- | --- |
| 槽位 1～4 | SaveUI / SaveSlot_UI.OnSlotClicked(int)，分别填写 1～4 |
| 确认框“是/确认” | SaveUI / SaveSlot_UI.OnConfirmClicked() |
| 确认框“否/返回” | SaveUI / SaveSlot_UI.OnCancelConfirmClicked() |
| 槽位面板“返回” | SaveUI / SaveSlot_UI.OnBackClicked() |

读取模式空槽和无效槽不可点击。保存模式所有槽都可选，已有文件时提示覆盖。
保存成功关闭确认和槽位面板；保存失败保持面板并提示原因。

## 主菜单

Menu_UI 新字段 GameModePanel：绑定“开始新游戏/继续游戏”面板，默认隐藏。
旧 DataChoosePanel / DataEnterPanel 可留空，不再作为新流程的槽位面板。

| 按钮 | Menu_UI 函数 |
| --- | --- |
| 开始游玩 | OnStartGameClicked() |
| 开始新游戏 | OnNewGameClicked() |
| 继续游戏 | OnContinueGameClicked() |
| 游戏方式面板返回 | OnReturnButtonClicked() |
| 主菜单退出应用 | OnQuitClicked() |

没有有效存档时，“开始游玩”直接从教程开始；有有效存档时显示 GameModePanel。
开始新游戏不创建或覆盖槽位文件；第一次手动保存才选择槽位。
继续游戏的槽位弹窗返回时重新显示 GameModePanel。

## 设置

Settings_UI 新字段 Quit Confirm Panel / Quit Confirm Text：绑定退出确认弹窗及文字。
弹窗应覆盖设置面板并拦截后方操作。

| 按钮 | Settings_UI 函数 |
| --- | --- |
| 保存游戏 | OnSaveButtonClicked() |
| 退出游戏 | OnQuitButtonClicked() |
| 退出确认“确认” | OnConfirmQuitClicked() |
| 退出确认“返回” | OnCancelQuitClicked() |
| 关闭设置 | OnReturnButtonClicked() |

退出指关闭应用；编辑器中停止播放。不会自动保存游戏进度。
关闭设置/确认退出时只保存音量偏好，不写游戏槽位或结局解锁。

## 通关

NextButton 继续绑定 EndingController.OnNextButtonClicked()。
制作名单点“退出”会打开共用槽位面板；玩家确认保存后，该槽位标记“已通关”，再清理运行状态返回主菜单。
取消选择则留在制作名单。新游戏从未保存过也能选择任意槽位保存通关记录。
不再自动合并全局结局解锁；已有旧全局记录不会删除。

## 存档行为

- 支持出租屋、地点场景、地图保存，等待对话、离场和转场全部结束。
- 每次保存重新选槽位，不依赖之前读取或保存的槽位。
- 版本 5 保存实际场景/地点，读档恢复相同位置；出租屋恢复卧室/客厅。
- 版本 1～4 的旧进度在内存迁移，仍返回出租屋；不会在读取时改写原文件。
- 已通关槽位读取后进入 EndScene 回看结算，不从 Day8 开始。
- 槽位弹窗打开期间暂停自动剧情检查，关闭后再检查。

## 建议手动验收

1. 没有有效存档时开始游戏，确认没有创建槽位文件。
2. 剧情结束后在设置保存：选槽位、取消确认、确认保存，检查摘要。
3. 重新启动，确认开始游玩显示新游戏/继续游戏；空槽无响应。
4. 分别在地图、楼道和出租屋保存，确认读取后地点、人物、遗物、日期一致。
5. 有对话、离场或转场时保存入口应拒绝；弹窗打开时剧情不自动抢播。
6. 从未手动保存过的一轮通关，在制作名单退出时保存，检查“已通关”与结局 ID。
7. 在通关选槽时取消，应留在制作名单且不写盘；保存后回主菜单，游戏方式选择仍可用。
8. 设置退出选择返回时继续当前游戏，选择确认时退出且不覆盖槽位。
