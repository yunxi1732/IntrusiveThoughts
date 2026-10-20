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
关闭设置/确认退出不保存音量偏好；只有 ApplyVol 的 OnApplyVolumeClicked() 保存当前音量。

## 通关

NextButton 继续绑定 EndingController.OnNextButtonClicked()。
制作名单点“退出”会打开共用槽位面板；玩家确认保存后，该槽位标记“已通关”，再清理运行状态返回主菜单。
取消选择则留在制作名单。新游戏从未保存过也能选择任意槽位保存通关记录。
进入制作名单时自动写入全局 hasCompletedGame 和 achievedEndings，不依赖玩家是否选槽保存。
achievedEndings 保存 endingId、title、image、priority、firstAchievedOrder；只收录 priority 0～3，按优先级升序、同组首次达成顺序排序，重复结局不重复添加。
此次全局写入保留上次已应用的音量偏好，不采集尚未 ApplyVol 的当前音量。新游戏、读取/覆盖/删除单个槽位不重置全局记录；清除全部存档会重置。

## 存档行为

回忆面板 Memory_UI 在启动读取全局进度后、通关记录自动保存成功后刷新，打开/关闭面板不重新加载列表。
主菜单 Memory 整个入口（含装饰图）仅在 HasCompletedGame 为 true 时显示。
Content 中的 EndList 由编辑器手动摆放（目前 11 个），代码只更新已有条目，不生成条目，也不修改 Content 高度和布局；条目数应等于 CSV-End 中 priority 0～3 的结局数量。
已达成项按全局顺序排列在前，使用当前 CSV 标题和图片，其余项图片透明且保留各场景条目的初始文字。
两个按钮由玩家手动配置 Inspector On Click：将 MenuController 拖入，MemoryButton 选 Memory_UI.OnMemoryClicked()，MemoryBackButton 选 Memory_UI.OnBackClicked()。
回忆滚动区域使用 MemoryScrollView：鼠标向上拖动浏览下方条目，向下拖动返回上方，禁用滚轮。Content 高度在场景中设为 4070（11 × 370），Viewport 高度 1100；代码不动态调整高度，增减条目时需同步调整场景数值。

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
7. 在通关选槽时取消，应留在制作名单且不写槽位；全局通关记录已在制作名单出现时保存。保存后回主菜单，游戏方式选择仍可用。
8. 多轮达成不同结局，重启后确认全局通关标记仍为 true、结局无重复，priority 为 0→3，同组保留首次达成顺序；未应用的音量不随通关保存。
9. 设置退出选择返回时继续当前游戏，选择确认时退出且不覆盖槽位。
