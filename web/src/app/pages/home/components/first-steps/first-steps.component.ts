import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
    FormBuilder,
    FormGroup,
    Validators,
    NgForm,
    ReactiveFormsModule,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { GameService } from 'src/app/service/game/game.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { LocaleService } from 'src/app/core/services/locale.service';
import { NewGame } from 'src/app/types/game/new-game';
import { LoadingComponent } from 'src/app/shared/loading/loading.component';
import { AuthService } from 'src/app/service/auth/auth.service';

@Component({
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatSelectModule,
        MatSliderModule,
        MatCardModule,
        MatIconModule,
        MatTooltipModule,
        LoadingComponent,
    ],
    selector: 'app-first-steps',
    templateUrl: './first-steps.component.html',
    styleUrls: ['./first-steps.component.scss'],
})
export class FirstStepsComponent {
    @Output() gameCreated = new EventEmitter<string>();

    @ViewChild('gameNgForm') gameNgForm!: NgForm;

    gameFormGroup: FormGroup;

    gameLanguages: { name: string; value: string; abbreviation: string }[] = [
        {
            name: $localize`Portuguese`,
            value: 'Portuguese',
            abbreviation: 'PT',
        },
        { name: $localize`English`, value: 'English', abbreviation: 'EN' },
        { name: $localize`Spanish`, value: 'Spanish', abbreviation: 'ES' },
    ];

    races: { name: string; value: string }[] = [
        { name: $localize`Human`, value: 'Human' },
        { name: $localize`Elf`, value: 'Elf' },
        { name: $localize`Dwarf`, value: 'Dwarf' },
        { name: $localize`Half Elf`, value: 'HalfElf' },
        { name: $localize`Halfling`, value: 'Halfling' },
        { name: $localize`Tiefling`, value: 'Tiefling' },
        { name: $localize`Dragonborn`, value: 'Dragonborn' },
        { name: $localize`Half Orc`, value: 'HalfOrc' },
        { name: $localize`Gnome`, value: 'Gnome' },
    ];

    skills: { name: string; value: string }[] = [
        { name: $localize`Strength`, value: 'Strength' },
        { name: $localize`Dexterity`, value: 'Dexterity' },
        { name: $localize`Intelligence`, value: 'Intelligence' },
        { name: $localize`Constitution`, value: 'Constitution' },
        { name: $localize`Charisma`, value: 'Charisma' },
        { name: $localize`Wisdom`, value: 'Wisdom' },
    ];

    racialBonus: { name: string; bonus: Record<string, number> }[] = [
        {
            name: 'Human',
            bonus: {
                Strength: 1,
                Dexterity: 1,
                Intelligence: 1,
                Constitution: 1,
                Charisma: 1,
                Wisdom: 1,
            },
        },
        { name: 'Elf', bonus: { Dexterity: 2 } },
        { name: 'Dwarf', bonus: { Constitution: 2 } },
        { name: 'HalfElf', bonus: { Intelligence: 1, Charisma: 2, Wisdom: 1 } },
        { name: 'Halfling', bonus: { Dexterity: 2 } },
        { name: 'Tiefling', bonus: { Intelligence: 1, Charisma: 2 } },
        { name: 'Dragonborn', bonus: { Strength: 2, Charisma: 1 } },
        { name: 'HalfOrc', bonus: { Strength: 2, Constitution: 1 } },
        { name: 'Gnome', bonus: { Intelligence: 2 } },
    ];

    loading: boolean = false;

    minSkillPoints: number = 8;
    maxSkillPointsToDistribute: number = 30;
    currentSkillPointsDistributed: number = 0;

    get isCommonUser(): boolean {
        return this.authService.getRole() === 'CommonUser';
    }

    constructor(
        private _formBuilder: FormBuilder,
        private gameService: GameService,
        private snackBar: SnackbarService,
        private localeService: LocaleService,
        private authService: AuthService,
    ) {
        this.gameFormGroup = this._formBuilder.group({
            gameNameControl: ['', Validators.required],
            characterNameControl: ['', Validators.required],
            characterDescriptionControl: ['', Validators.required],
            characterRaceControl: ['', Validators.required],
            gameLanguageControl: [
                this.getDefaultGameLanguage(),
                Validators.required,
            ],
            skillsGroup: this._formBuilder.group({
                Strength: [8, Validators.required],
                Dexterity: [8, Validators.required],
                Constitution: [8, Validators.required],
                Intelligence: [8, Validators.required],
                Wisdom: [8, Validators.required],
                Charisma: [8, Validators.required],
            }),
        });

        const skillsGroup = this.gameFormGroup.get('skillsGroup') as FormGroup;
        skillsGroup.valueChanges.subscribe(() => {
            this.sumSkillPoints();
        });

        this.sumSkillPoints();
    }

    onSubmit(): void {
        if (this.isCommonUser) return;

        this.loading = true;
        let newGame: NewGame = {
            protagonistDescription: this.gameFormGroup.get(
                'characterDescriptionControl',
            )?.value,
            protagonistName: this.gameFormGroup.get('characterNameControl')
                ?.value,
            name: this.gameFormGroup.get('gameNameControl')?.value,
            protagonistRace: this.gameFormGroup.get('characterRaceControl')
                ?.value,
            gameLanguage: this.gameFormGroup.get('gameLanguageControl')?.value,
            skills: {
                Strength: this.gameFormGroup.get('skillsGroup.Strength')?.value,
                Dexterity: this.gameFormGroup.get('skillsGroup.Dexterity')
                    ?.value,
                Constitution: this.gameFormGroup.get('skillsGroup.Constitution')
                    ?.value,
                Intelligence: this.gameFormGroup.get('skillsGroup.Intelligence')
                    ?.value,
                Wisdom: this.gameFormGroup.get('skillsGroup.Wisdom')?.value,
                Charisma: this.gameFormGroup.get('skillsGroup.Charisma')?.value,
            },
        };

        this.gameService.newGame(newGame).subscribe({
            next: () => {
                this.gameCreated.emit(newGame.name);
                this.loading = false;
                this.snackBar.addSuccess(
                    $localize`Game '${newGame.name}' created!`,
                );
            },
            error: (error: HttpErrorResponse) => {
                //TODO: Exibir erro e tratar
                this.loading = false;
                this.snackBar.addError(
                    $localize`Something went wrong while attempting to create the game. Verify with the admin if you have the permissions.`,
                );
            },
        });
    }

    sumSkillPoints(): void {
        const skillsGroup = this.gameFormGroup.get('skillsGroup') as FormGroup;
        let sum = 0;

        Object.keys(skillsGroup.controls).forEach((skillName) => {
            const skill = skillsGroup.get(skillName);
            sum += (skill?.value || 0) - this.minSkillPoints;
        });

        this.currentSkillPointsDistributed = sum;
    }

    getRacialBonusByRaceAndSkill(race: string, skill: string): number {
        const racialBonus = this.racialBonus.find(
            (bonus) => bonus.name === race,
        );

        return racialBonus?.bonus[skill] || 0;
    }

    private getDefaultGameLanguage(): string {
        const systemLang = this.localeService.getCurrentLanguage();
        const langMap: Record<string, string> = {
            en: 'English',
            'pt-BR': 'Portuguese',
            es: 'Spanish',
        };
        return langMap[systemLang] || 'English';
    }
}
